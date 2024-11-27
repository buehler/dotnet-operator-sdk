using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;
using KubeOps.Transpiler.Kubernetes;

namespace KubeOps.Transpiler;

/// <summary>
/// CRD transpiler for Kubernetes entities.
/// </summary>
public static class Crds
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
    private const string Decimal = "decimal";
    private const string DateTime = "date-time";

    private static readonly string[] IgnoredToplevelProperties = ["metadata", "apiversion", "kind"];

    /// <summary>
    /// Transpile a single type to a CRD.
    /// </summary>
    /// <param name="context">The <see cref="MetadataLoadContext"/>.</param>
    /// <param name="type">The type to convert.</param>
    /// <returns>The converted custom resource definition.</returns>
    public static V1CustomResourceDefinition Transpile(this MetadataLoadContext context, Type type)
    {
        type = context.GetContextType(type);
        var (meta, scope) = context.ToEntityMetadata(type);
        var crd = new V1CustomResourceDefinition(new()).Initialize();

        crd.Metadata.Name = $"{meta.PluralName}.{meta.Group}";
        crd.Spec.Group = meta.Group;

        crd.Spec.Names =
            new V1CustomResourceDefinitionNames
            {
                Kind = meta.Kind,
                ListKind = meta.ListKind,
                Singular = meta.SingularName,
                Plural = meta.PluralName,
            };
        crd.Spec.Scope = scope;
        if (type.GetCustomAttributeData<KubernetesEntityShortNamesAttribute>()?.ConstructorArguments[0].Value is
            ReadOnlyCollection<CustomAttributeTypedArgument> shortNames)
        {
            crd.Spec.Names.ShortNames = shortNames.Select(a => a.Value?.ToString()).ToList();
        }

        var version = new V1CustomResourceDefinitionVersion(meta.Version, true, true);
        if
            (type.GetProperty("Status") != null
             || type.GetProperty("status") != null)
        {
            version.Subresources = new V1CustomResourceSubresources(null, new object());
        }

        version.Schema = new V1CustomResourceValidation(new V1JSONSchemaProps
        {
            Type = Object,
            Description =
                type.GetCustomAttributeData<DescriptionAttribute>()?.GetCustomAttributeCtorArg<string>(context, 0),
            Properties = type.GetProperties()
                .Where(p => !IgnoredToplevelProperties.Contains(p.Name.ToLowerInvariant())
                            && p.GetCustomAttributeData<IgnoreAttribute>() == null)
                .Select(p => (Name: p.GetPropertyName(context), Schema: context.Map(p)))
                .ToDictionary(t => t.Name, t => t.Schema),
        });

        version.AdditionalPrinterColumns = context.MapPrinterColumns(type).ToList() switch
        {
            { Count: > 0 } l => l,
            _ => null,
        };
        crd.Spec.Versions = new List<V1CustomResourceDefinitionVersion> { version };
        crd.Validate();

        return crd;
    }

    /// <summary>
    /// Transpile a list of entities to CRDs and group them by version.
    /// </summary>
    /// <param name="context">The <see cref="MetadataLoadContext"/>.</param>
    /// <param name="types">The types to convert.</param>
    /// <returns>The converted custom resource definitions.</returns>
    public static IEnumerable<V1CustomResourceDefinition> Transpile(
        this MetadataLoadContext context,
        IEnumerable<Type> types)
        => types
            .Select(context.GetContextType)
            .Where(type => type.Assembly != context.GetContextType<KubernetesEntityAttribute>().Assembly
                           && type.GetCustomAttributesData<KubernetesEntityAttribute>().Any()
                           && !type.GetCustomAttributesData<IgnoreAttribute>().Any())
            .Select(type => (Props: context.Transpile(type),
                IsStorage: type.GetCustomAttributesData<StorageVersionAttribute>().Any()))
            .GroupBy(grp => grp.Props.Metadata.Name)
            .Select(
                group =>
                {
                    if (group.Count(def => def.IsStorage) > 1)
                    {
                        throw new ArgumentException("There are multiple stored versions on an entity.");
                    }

                    var crd = group.First().Props;
                    crd.Spec.Versions = group
                        .SelectMany(
                            c => c.Props.Spec.Versions.Select(
                                v =>
                                {
                                    v.Served = true;
                                    v.Storage = c.IsStorage;
                                    return v;
                                }))
                        .OrderByDescending(v => v.Name, new KubernetesVersionComparer())
                        .ToList();

                    // when only one version exists, or when no StorageVersion attributes are found
                    // the first version in the list is the stored one.
                    if (crd.Spec.Versions.Count == 1 || !group.Any(def => def.IsStorage))
                    {
                        crd.Spec.Versions[0].Storage = true;
                    }

                    return crd;
                });

    private static string GetPropertyName(this PropertyInfo prop, MetadataLoadContext context)
    {
        var name = prop.GetCustomAttributeData<JsonPropertyNameAttribute>() switch
        {
            null => prop.Name,
            { } attr => attr.GetCustomAttributeCtorArg<string>(context, 0) ?? prop.Name,
        };

        return $"{name[..1].ToLowerInvariant()}{name[1..]}";
    }

    private static IEnumerable<V1CustomResourceColumnDefinition> MapPrinterColumns(
        this MetadataLoadContext context,
        Type type)
    {
        var props = type.GetProperties().Select(p => (Prop: p, Path: string.Empty)).ToList();
        while (props.Count > 0)
        {
            var (prop, path) = props[0];
            props.RemoveAt(0);

            if (prop.PropertyType.IsClass)
            {
                props.AddRange(prop.PropertyType.GetProperties()
                    .Select(p => (Prop: p, Path: $"{path}.{prop.GetPropertyName(context)}")));
            }

            if (prop.GetCustomAttributeData<AdditionalPrinterColumnAttribute>() is not { } attr)
            {
                continue;
            }

            var mapped = context.Map(prop);
            yield return new V1CustomResourceColumnDefinition
            {
                Name = attr.GetCustomAttributeCtorArg<string>(context, 1) ?? prop.GetPropertyName(context),
                JsonPath = $"{path}.{prop.GetPropertyName(context)}",
                Type = mapped.Type,
                Description = mapped.Description,
                Format = mapped.Format,
                Priority = attr.GetCustomAttributeCtorArg<PrinterColumnPriority>(context, 0) switch
                {
                    PrinterColumnPriority.StandardView => 0,
                    _ => 1,
                },
            };
        }

        foreach (var attr in type.GetCustomAttributesData<GenericAdditionalPrinterColumnAttribute>())
        {
            yield return new V1CustomResourceColumnDefinition
            {
                Name = attr.GetCustomAttributeCtorArg<string>(context, 1),
                JsonPath = attr.GetCustomAttributeCtorArg<string>(context, 0),
                Type = attr.GetCustomAttributeCtorArg<string>(context, 2),
                Description = attr.GetCustomAttributeNamedArg<string>(context, "Description"),
                Format = attr.GetCustomAttributeNamedArg<string>(context, "Format"),
                Priority = attr.GetCustomAttributeNamedArg<PrinterColumnPriority>(context, "Priority") switch
                {
                    PrinterColumnPriority.StandardView => 0,
                    _ => 1,
                },
            };
        }
    }

    private static V1JSONSchemaProps Map(this MetadataLoadContext context, PropertyInfo prop)
    {
        var props = context.Map(prop.PropertyType);

        props.Description ??= prop.GetCustomAttributeData<DescriptionAttribute>()
            ?.GetCustomAttributeCtorArg<string>(context, 0);

        props.Nullable = prop.IsNullable();

        if (prop.GetCustomAttributeData<ExternalDocsAttribute>() is { } extDocs)
        {
            props.ExternalDocs = new V1ExternalDocumentation(
                extDocs.GetCustomAttributeCtorArg<string>(context, 0),
                extDocs.GetCustomAttributeCtorArg<string>(context, 1));
        }

        if (prop.GetCustomAttributeData<ItemsAttribute>() is { } items)
        {
            props.MinItems = items.GetCustomAttributeCtorArg<long>(context, 0);
            props.MaxItems = items.GetCustomAttributeCtorArg<long>(context, 1);
        }

        if (prop.GetCustomAttributeData<LengthAttribute>() is { } length)
        {
            props.MinLength = length.GetCustomAttributeCtorArg<long>(context, 0);
            props.MaxLength = length.GetCustomAttributeCtorArg<long>(context, 1);
        }

        if (prop.GetCustomAttributeData<MultipleOfAttribute>() is { } multi)
        {
            props.MultipleOf = multi.GetCustomAttributeCtorArg<double>(context, 0);
        }

        if (prop.GetCustomAttributeData<PatternAttribute>() is { } pattern)
        {
            props.Pattern = pattern.GetCustomAttributeCtorArg<string>(context, 0);
        }

        if (prop.GetCustomAttributeData<RangeMaximumAttribute>() is { } rangeMax)
        {
            props.Maximum = rangeMax.GetCustomAttributeCtorArg<double>(context, 0);
            props.ExclusiveMaximum =
                rangeMax.GetCustomAttributeCtorArg<bool>(context, 1);
        }

        if (prop.GetCustomAttributeData<RangeMinimumAttribute>() is { } rangeMin)
        {
            props.Minimum = rangeMin.GetCustomAttributeCtorArg<double>(context, 0);
            props.ExclusiveMinimum =
                rangeMin.GetCustomAttributeCtorArg<bool>(context, 1);
        }

        if (prop.GetCustomAttributeData<PreserveUnknownFieldsAttribute>() is not null)
        {
            props.XKubernetesPreserveUnknownFields = true;
        }

        if (prop.GetCustomAttributeData<EmbeddedResourceAttribute>() is not null)
        {
            props.XKubernetesEmbeddedResource = true;
            props.XKubernetesPreserveUnknownFields = true;
            props.Type = Object;
            props.Properties = null;
        }

        return props;
    }

    private static V1JSONSchemaProps Map(this MetadataLoadContext context, Type type)
    {
        if (type.FullName == "System.String")
        {
            return new V1JSONSchemaProps { Type = String, Nullable = false };
        }

        if (type.Name == typeof(Nullable<>).Name && type.GenericTypeArguments.Length == 1)
        {
            var props = context.Map(type.GenericTypeArguments[0]);
            props.Nullable = true;
            return props;
        }

        var interfaces = (type.IsInterface
            ? type.GetInterfaces().Append(type)
            : type.GetInterfaces()).ToList();

        var interfaceNames = interfaces.Select(i =>
            i.IsGenericType
                ? i.GetGenericTypeDefinition().FullName
                : i.FullName).ToList();

        if (interfaceNames.Contains(typeof(IDictionary<,>).FullName))
        {
            var dictionaryImpl = interfaces
                .First(i => i.IsGenericType
                            && i.GetGenericTypeDefinition().FullName == typeof(IDictionary<,>).FullName);

            var additionalProperties = context.Map(dictionaryImpl.GenericTypeArguments[1]);
            return new V1JSONSchemaProps
            {
                Type = Object,
                AdditionalProperties = additionalProperties,
                Nullable = false,
            };
        }

        if (interfaceNames.Contains(typeof(IDictionary).FullName))
        {
            return new V1JSONSchemaProps { Type = Object, XKubernetesPreserveUnknownFields = true, Nullable = false, };
        }

        if (interfaceNames.Contains(typeof(IEnumerable<>).FullName))
        {
            return context.MapEnumerationType(type, interfaces);
        }

        if (type.BaseType?.Name == nameof(CustomKubernetesEntity) || type.BaseType?.Name == typeof(CustomKubernetesEntity<>).Name)
        {
            return context.MapObjectType(type);
        }

        return type.BaseType?.FullName switch
        {
            "System.Object" => context.MapObjectType(type),
            "System.ValueType" => context.MapValueType(type),
            "System.Enum" => new V1JSONSchemaProps
            {
                Type = String,
                EnumProperty = Enum.GetNames(type).Cast<object>().ToList(),
            },
            _ => throw InvalidType(type),
        };
    }

    private static V1JSONSchemaProps MapObjectType(this MetadataLoadContext context, Type type)
    {
        switch (type.FullName)
        {
            case "k8s.Models.V1ObjectMeta":
                return new V1JSONSchemaProps { Type = Object, Nullable = false };
            case "k8s.Models.IntstrIntOrString":
                return new V1JSONSchemaProps { XKubernetesIntOrString = true, Nullable = false };
            default:
                if (context.GetContextType<IKubernetesObject>().IsAssignableFrom(type) &&
                    type is { IsAbstract: false, IsInterface: false } &&
                    type.Assembly == context.GetContextType<IKubernetesObject>().Assembly)
                {
                    return new V1JSONSchemaProps
                    {
                        Type = Object,
                        Properties = null,
                        XKubernetesPreserveUnknownFields = true,
                        XKubernetesEmbeddedResource = true,
                        Nullable = false,
                    };
                }

                return new V1JSONSchemaProps
                {
                    Type = Object,
                    Description =
                        type.GetCustomAttributeData<DescriptionAttribute>()
                            ?.GetCustomAttributeCtorArg<string>(context, 0),
                    Properties = type
                        .GetProperties()
                        .Where(p => p.GetCustomAttributeData<IgnoreAttribute>() == null)
                        .Select(p => (Name: p.GetPropertyName(context), Schema: context.Map(p)))
                        .ToDictionary(t => t.Name, t => t.Schema),
                    Required = type.GetProperties()
                            .Where(p => p.GetCustomAttributeData<RequiredAttribute>() != null
                                        && p.GetCustomAttributeData<IgnoreAttribute>() == null)
                            .Select(p => p.GetPropertyName(context))
                            .ToList() switch
                    {
                        { Count: > 0 } p => p,
                        _ => null,
                    },
                };
        }
    }

    private static V1JSONSchemaProps MapEnumerationType(
        this MetadataLoadContext context,
        Type type,
        IEnumerable<Type> interfaces)
    {
        Type? enumerableType = interfaces
            .FirstOrDefault(i => i.IsGenericType
                                 && i.GetGenericTypeDefinition().FullName == typeof(IEnumerable<>).FullName
                                 && i.GenericTypeArguments.Length == 1);

        if (enumerableType == null)
        {
            throw InvalidType(type);
        }

        Type listType = enumerableType.GenericTypeArguments[0];
        if (listType.IsGenericType && listType.GetGenericTypeDefinition().FullName == typeof(KeyValuePair<,>).FullName)
        {
            var additionalProperties = context.Map(listType.GenericTypeArguments[1]);
            return new V1JSONSchemaProps
            {
                Type = Object,
                AdditionalProperties = additionalProperties,
                Nullable = false,
            };
        }

        var items = context.Map(listType);
        return new V1JSONSchemaProps { Type = Array, Items = items, Nullable = false };
    }

    private static V1JSONSchemaProps MapValueType(this MetadataLoadContext _, Type type) =>
        type.FullName switch
        {
            "System.Int32" => new V1JSONSchemaProps { Type = Integer, Format = Int32, Nullable = false },
            "System.Int64" => new V1JSONSchemaProps { Type = Integer, Format = Int64, Nullable = false },
            "System.Single" => new V1JSONSchemaProps { Type = Number, Format = Float, Nullable = false },
            "System.Double" => new V1JSONSchemaProps { Type = Number, Format = Double, Nullable = false },
            "System.Decimal" => new V1JSONSchemaProps { Type = Number, Format = Decimal, Nullable = false },
            "System.Boolean" => new V1JSONSchemaProps { Type = Boolean, Nullable = false },
            "System.DateTime" => new V1JSONSchemaProps { Type = String, Format = DateTime, Nullable = false },
            "System.DateTimeOffset" => new V1JSONSchemaProps { Type = String, Format = DateTime, Nullable = false },
            _ => throw InvalidType(type),
        };

    private static ArgumentException InvalidType(Type type) =>
        new($"The given type {type.FullName} is not a valid Kubernetes entity.");
}
