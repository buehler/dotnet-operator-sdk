using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

using Namotion.Reflection;

namespace KubeOps.Transpiler.Crds;

public static class CrdTranspiler
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

    public static V1CustomResourceDefinition Create(Type entityType)
    {
        var entityAttribute = entityType.GetCustomAttribute<KubernetesEntityAttribute>();
        if (entityAttribute == null)
        {
            throw new ArgumentException("The given type is not a valid Kubernetes entity.", nameof(entityType));
        }

        var entityMetadata = new EntityMetadata(
            entityAttribute.Kind,
            entityAttribute.ApiVersion,
            string.IsNullOrWhiteSpace(entityAttribute.Group) ? null : entityAttribute.Group,
            string.IsNullOrWhiteSpace(entityAttribute.PluralName) ? null : entityAttribute.PluralName);
        var entityScope = entityType.GetCustomAttribute<EntityScopeAttribute>()?.Scope.ToString() ??
                          default(EntityScope).ToString();

        var crd = new V1CustomResourceDefinition(new()).Initialize();
        crd.Metadata.Name = $"{entityMetadata.PluralName}.{entityMetadata.Group}";

        var shortNames = entityType.GetCustomAttributes<KubernetesEntityShortNamesAttribute>(true)
            .Aggregate(
                new List<string>(),
                (list, attribute) =>
                {
                    list.AddRange(attribute.ShortNames);
                    return list;
                });

        var spec = crd.Spec;
        spec.Group = entityMetadata.Group;
        spec.Names = new V1CustomResourceDefinitionNames
        {
            Kind = entityMetadata.Kind,
            ListKind = entityMetadata.ListKind,
            Singular = entityMetadata.SingularName,
            Plural = entityMetadata.PluralName,
            ShortNames = shortNames.Any() ? shortNames : null,
        };
        spec.Scope = entityScope;

        var version = new V1CustomResourceDefinitionVersion(entityMetadata.Version, true, true);
        if (entityType.GetProperty("Status") != null || entityType.GetProperty("status") != null)
        {
            version.Subresources = new V1CustomResourceSubresources(null, new object());
        }

        var columns = new List<V1CustomResourceColumnDefinition>();
        version.Schema =
            new V1CustomResourceValidation(MapType(entityType, columns, string.Empty));
        version.AdditionalPrinterColumns = entityType
            .GetCustomAttributes<GenericAdditionalPrinterColumnAttribute>(true)
            .Select(a => new V1CustomResourceColumnDefinition
            {
                Name = a.Name,
                JsonPath = a.JsonPath,
                Type = a.Type,
                Description = a.Description,
                Format = a.Format,
                Priority = a.Priority switch
                {
                    PrinterColumnPriority.StandardView => 0,
                    _ => 1,
                },
            })
            .Concat(columns)
            .ToList();

        if (version.AdditionalPrinterColumns.Count == 0)
        {
            version.AdditionalPrinterColumns = null;
        }

        spec.Versions = new[] { version };

        crd.Validate();
        return crd;
    }

    private static V1JSONSchemaProps MapProperty(
        PropertyInfo info,
        IList<V1CustomResourceColumnDefinition> additionalColumns,
        string jsonPath)
    {


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
                    Priority = additionalColumn.Priority switch
                    {
                        PrinterColumnPriority.StandardView => 0,
                        _ => 1,
                    },
                    Type = props.Type,
                    Format = props.Format,
                });
        }

        return props;
    }


}
