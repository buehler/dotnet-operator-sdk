using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient.Entities;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Errors;
using KubeOps.Operator.Util;
using Namotion.Reflection;

namespace KubeOps.Operator.Entities.Extensions;

internal static class EntityToCrdExtensions
{
    private const string Integer = "integer";
    private const string Number = "number";
    private const string String = "string";
    private const string Boolean = "boolean";
    private const string Object = "object";
    private const string Array = "array";

    private const string Int32 = "int32";
    private const string Int64 = "int64";
    private const string Float = "float";
    private const string Double = "double";
    private const string DateTime = "date-time";

    private static readonly string[] IgnoredToplevelProperties = { "metadata", "apiversion", "kind" };

    internal static V1CustomResourceDefinition CreateCrd(
        this IKubernetesObject<V1ObjectMeta> kubernetesEntity) => CreateCrd(kubernetesEntity.GetType());

    internal static V1CustomResourceDefinition CreateCrd<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta> => CreateCrd(typeof(TEntity));

    internal static V1CustomResourceDefinition CreateCrd(this Type entityType)
    {
        var entityDefinition = entityType.ToEntityDefinition();

        var crd = new V1CustomResourceDefinition(
            new V1CustomResourceDefinitionSpec(),
            $"{V1CustomResourceDefinition.KubeGroup}/{V1CustomResourceDefinition.KubeApiVersion}",
            V1CustomResourceDefinition.KubeKind,
            new V1ObjectMeta { Name = $"{entityDefinition.Plural}.{entityDefinition.Group}" });

        var shortNames = entityType.GetCustomAttributes<KubernetesEntityShortNamesAttribute>(true)
            .Aggregate(
                new List<string>(),
                (list, attribute) =>
                {
                    list.AddRange(attribute.ShortNames);
                    return list;
                });

        var spec = crd.Spec;
        spec.Group = entityDefinition.Group;
        spec.Names = new V1CustomResourceDefinitionNames
        {
            Kind = entityDefinition.Kind,
            ListKind = entityDefinition.ListKind,
            Singular = entityDefinition.Singular,
            Plural = entityDefinition.Plural,
            ShortNames = shortNames.Any() ? shortNames : null,
        };
        spec.Scope = entityDefinition.Scope.ToString();

        var version = new V1CustomResourceDefinitionVersion();
        spec.Versions = new[] { version };

        version.Name = entityDefinition.Version;
        version.Served = true;
        version.Storage = true;

        if (entityType.GetProperty("Status") != null || entityType.GetProperty("status") != null)
        {
            version.Subresources = new V1CustomResourceSubresources(null, new object());
        }

        var columns = new List<V1CustomResourceColumnDefinition>();
        version.Schema = new V1CustomResourceValidation(MapType(entityType, columns, string.Empty));

        version.AdditionalPrinterColumns = entityType
            .GetCustomAttributes<GenericAdditionalPrinterColumnAttribute>(true)
            .Select(a => a.ToAdditionalPrinterColumn())
            .Concat(columns)
            .ToList();

        if (version.AdditionalPrinterColumns.Count == 0)
        {
            version.AdditionalPrinterColumns = null;
        }

        return crd;
    }

    private static V1JSONSchemaProps MapProperty(
        PropertyInfo info,
        IList<V1CustomResourceColumnDefinition> additionalColumns,
        string jsonPath)
    {
        V1JSONSchemaProps props;
        try
        {
            props = MapType(info.PropertyType, additionalColumns, jsonPath);
        }
        catch (Exception ex)
        {
            throw new CrdConversionException(
                $@"During conversion of the property ""{info.Name}"" with type ""{info.PropertyType.Name}"", an error occured.",
                ex);
        }

        var contextual = info.ToContextualProperty();

        // Check for nullability and nullable types
        if (contextual.Nullability == Nullability.Nullable)
        {
            props.Nullable = true;
        }

        // Get the description on the property
        props.Description ??= info.GetCustomAttribute<DescriptionAttribute>()?.Description;
        var docsSummary = contextual.GetXmlDocsSummary();
        if (!string.IsNullOrWhiteSpace(docsSummary))
        {
            props.Description ??= docsSummary;
        }

        var externalDoc = info.GetCustomAttribute<ExternalDocsAttribute>();
        if (externalDoc != null)
        {
            props.ExternalDocs = new V1ExternalDocumentation(externalDoc.Description, externalDoc.Url);
        }

        // Get items (of array probably) description
        var items = info.GetCustomAttribute<ItemsAttribute>();
        if (items != null)
        {
            props.MinItems = items.MinItems == -1 ? null : items.MinItems;
            props.MaxItems = items.MaxItems == -1 ? null : items.MaxItems;
        }

        // Get length description
        var length = info.GetCustomAttribute<LengthAttribute>();
        if (length != null)
        {
            props.MinLength = length.MinLength == -1 ? null : length.MinLength;
            props.MaxLength = length.MaxLength == -1 ? null : length.MaxLength;
        }

        // get multiple of description
        var multipleOf = info.GetCustomAttribute<MultipleOfAttribute>();
        if (multipleOf != null)
        {
            props.MultipleOf = multipleOf.Value;
        }

        // get pattern description
        var pattern = info.GetCustomAttribute<PatternAttribute>();
        if (pattern != null)
        {
            props.Pattern = pattern.RegexPattern;
        }

        // get range max description
        var rangeMax = info.GetCustomAttribute<RangeMaximumAttribute>();
        if (rangeMax != null)
        {
            props.Maximum = rangeMax.Maximum;
            props.ExclusiveMaximum = rangeMax.ExclusiveMaximum;
        }

        // get range min description
        var rangeMin = info.GetCustomAttribute<RangeMinimumAttribute>();
        if (rangeMin != null)
        {
            props.Minimum = rangeMin.Minimum;
            props.ExclusiveMinimum = rangeMin.ExclusiveMinimum;
        }

        // check if preserve unknown is set
        if (info.GetCustomAttribute<PreserveUnknownFieldsAttribute>() != null)
        {
            props.XKubernetesPreserveUnknownFields = true;
        }

        // check if embedded resource is set
        if (info.GetCustomAttribute<EmbeddedResourceAttribute>() != null)
        {
            SetEmbeddedResourceProperties(props);
        }

        // get additional printer column information
        var additionalColumn = info.GetCustomAttribute<AdditionalPrinterColumnAttribute>();
        if (additionalColumn != null)
        {
            additionalColumns.Add(
                new V1CustomResourceColumnDefinition
                {
                    Name = additionalColumn.Name ?? info.Name,
                    Description = props.Description,
                    JsonPath = jsonPath,
                    Priority = additionalColumn.Priority,
                    Type = props.Type,
                    Format = props.Format,
                });
        }

        return props;
    }

    private static V1JSONSchemaProps MapType(
        Type type,
        IList<V1CustomResourceColumnDefinition> additionalColumns,
        string jsonPath)
    {
        var props = new V1JSONSchemaProps();

        // this description is on the class
        props.Description ??= type.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault()?.Description;

        var isSimpleType = IsSimpleType(type);

        if (type == typeof(V1ObjectMeta))
        {
            props.Type = Object;
        }
        else if (type.IsArray)
        {
            props.Type = Array;
            props.Items = MapType(
                type.GetElementType() ?? throw new NullReferenceException("No Array Element Type found"),
                additionalColumns,
                jsonPath);
        }
        else if (!isSimpleType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            var genericTypes = type.GenericTypeArguments;
            props.Type = Object;
            props.AdditionalProperties = MapType(genericTypes[1], additionalColumns, jsonPath);
        }
        else if (!isSimpleType &&
                 type.IsGenericType &&
                 type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                 type.GenericTypeArguments.Length == 1 &&
                 type.GenericTypeArguments.Single().IsGenericType &&
                 type.GenericTypeArguments.Single().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            var genericTypes = type.GenericTypeArguments.Single().GenericTypeArguments;
            props.Type = Object;
            props.AdditionalProperties = MapType(genericTypes[1], additionalColumns, jsonPath);
        }
        else if (!isSimpleType &&
                 (typeof(IDictionary).IsAssignableFrom(type) ||
                  (type.IsGenericType &&
                   type.GetGenericArguments().FirstOrDefault()?.IsGenericType == true &&
                   type.GetGenericArguments().FirstOrDefault()?.GetGenericTypeDefinition() ==
                   typeof(KeyValuePair<,>))))
        {
            props.Type = Object;
            props.XKubernetesPreserveUnknownFields = true;
        }
        else if (!isSimpleType && IsGenericEnumerableType(type, out Type? closingType))
        {
            props.Type = Array;
            props.Items = MapType(closingType, additionalColumns, jsonPath);
        }
        else if (type == typeof(IntstrIntOrString))
        {
            props.XKubernetesIntOrString = true;
        }
        else if (typeof(IKubernetesObject).IsAssignableFrom(type) &&
                 !type.IsAbstract &&
                 !type.IsInterface &&
                 type.Assembly == typeof(IKubernetesObject).Assembly)
        {
            SetEmbeddedResourceProperties(props);
        }
        else if (!isSimpleType)
        {
            ProcessType(type, props, additionalColumns, jsonPath);
        }
        else if (type == typeof(int) || Nullable.GetUnderlyingType(type) == typeof(int))
        {
            props.Type = Integer;
            props.Format = Int32;
        }
        else if (type == typeof(long) || Nullable.GetUnderlyingType(type) == typeof(long))
        {
            props.Type = Integer;
            props.Format = Int64;
        }
        else if (type == typeof(float) || Nullable.GetUnderlyingType(type) == typeof(float))
        {
            props.Type = Number;
            props.Format = Float;
        }
        else if (type == typeof(double) || Nullable.GetUnderlyingType(type) == typeof(double))
        {
            props.Type = Number;
            props.Format = Double;
        }
        else if (type == typeof(string) || Nullable.GetUnderlyingType(type) == typeof(string))
        {
            props.Type = String;
        }
        else if (type == typeof(bool) || Nullable.GetUnderlyingType(type) == typeof(bool))
        {
            props.Type = Boolean;
        }
        else if (type == typeof(DateTime) || Nullable.GetUnderlyingType(type) == typeof(DateTime))
        {
            props.Type = String;
            props.Format = DateTime;
        }
        else if (type.IsEnum)
        {
            props.Type = String;
            props.EnumProperty = new List<object>(Enum.GetNames(type));
        }
        else if (Nullable.GetUnderlyingType(type)?.IsEnum == true)
        {
            props.Type = String;
            props.EnumProperty = new List<object>(Enum.GetNames(Nullable.GetUnderlyingType(type)!));
        }
        else
        {
            throw new CrdPropertyTypeException();
        }

        return props;
    }

    private static void SetEmbeddedResourceProperties(V1JSONSchemaProps props)
    {
        props.Type = Object;
        props.Properties = null;
        props.XKubernetesPreserveUnknownFields = true;
        props.XKubernetesEmbeddedResource = true;
    }

    private static void ProcessType(
        Type type,
        V1JSONSchemaProps props,
        IList<V1CustomResourceColumnDefinition> additionalColumns,
        string jsonPath)
    {
        props.Type = Object;

        props.Properties = new Dictionary<string, V1JSONSchemaProps>(
            type.GetProperties()
                .Where(
                    info => jsonPath != string.Empty ||
                            !IgnoredToplevelProperties.Contains(info.Name.ToLowerInvariant()))
                .Where(info => info.GetCustomAttribute<IgnorePropertyAttribute>() == null)
                .Select(
                    prop => KeyValuePair.Create(
                        GetPropertyName(prop),
                        MapProperty(prop, additionalColumns, $"{jsonPath}.{GetPropertyName(prop)}"))));
        props.Required = type.GetProperties()
            .Where(
                prop => prop.GetCustomAttribute<RequiredAttribute>() != null &&
                        prop.GetCustomAttribute<IgnorePropertyAttribute>() == null)
            .Select(GetPropertyName)
            .ToList();
        if (props.Required.Count == 0)
        {
            props.Required = null;
        }
    }

    private static bool IsSimpleType(Type type) =>
        type.IsPrimitive ||
        new[]
        {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
        }.Contains(type) ||
        type.IsEnum ||
        Convert.GetTypeCode(type) != TypeCode.Object ||
        (type.IsGenericType &&
         type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
         IsSimpleType(type.GetGenericArguments()[0]));

    private static string GetPropertyName(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        var propertyName = attribute?.Name ?? property.Name;
        return propertyName.ToCamelCase();
    }

    private static bool IsGenericEnumerableType(Type type, [NotNullWhen(true)] out Type? closingType)
    {
        if (type.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
        {
            closingType = type.GetGenericArguments()[0];
            return true;
        }

        closingType = type
            .GetInterfaces()
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(t => t.GetGenericArguments()[0])
            .FirstOrDefault();

        return closingType != null;
    }
}
