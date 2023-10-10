using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace KubeOps.Cli.Transpilation;

internal static class Crds
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
                Kind = meta.Kind, ListKind = meta.ListKind, Singular = meta.SingularName, Plural = meta.PluralName,
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
                .Where(p => !IgnoredToplevelProperties.Contains(p.Name.ToLowerInvariant()))
                .Where(p => p.GetCustomAttributeData<IgnoreAttribute>() == null)
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

    public static IEnumerable<V1CustomResourceDefinition> Transpile(
        this MetadataLoadContext context,
        IEnumerable<Type> types)
        => types
            .Select(context.GetContextType)
            .Where(type => type.Assembly != context.GetContextType<KubernetesEntityAttribute>().Assembly)
            .Where(type => type.GetCustomAttributesData<KubernetesEntityAttribute>().Any())
            .Where(type => !type.GetCustomAttributesData<IgnoreAttribute>().Any())
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

    private static IEnumerable<V1CustomResourceColumnDefinition> MapPrinterColumns(this MetadataLoadContext context,
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

        // TODO: xml docs
        props.Description ??= prop.GetCustomAttributeData<DescriptionAttribute>()
            ?.GetCustomAttributeCtorArg<string>(context, 0);
        if (string.IsNullOrWhiteSpace(props.Description))
        {
            props.Description = null;
        }

        props.Nullable = new NullabilityInfoContext().Create(prop).ReadState == NullabilityState.Nullable ||
                         prop.PropertyType.FullName?.Contains("Nullable") == true;

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
        if (type == context.GetContextType<V1ObjectMeta>())
        {
            return new V1JSONSchemaProps { Type = Object };
        }

        if (type.IsArray && type.GetElementType() != null)
        {
            return new V1JSONSchemaProps { Type = Array, Items = context.Map(type.GetElementType()!) };
        }

        if (!IsSimpleType(type)
            && type.IsGenericType
            && type.GetGenericTypeDefinition() == context.GetContextType(typeof(IDictionary<,>))
            && type.GenericTypeArguments.Contains(context.GetContextType(typeof(ResourceQuantity))))
        {
            return new V1JSONSchemaProps { Type = Object, XKubernetesPreserveUnknownFields = true };
        }

        if (!IsSimpleType(type) &&
            type.IsGenericType &&
            type.GetGenericTypeDefinition() == context.GetContextType(typeof(IEnumerable<>)) &&
            type.GenericTypeArguments.Length == 1 &&
            type.GenericTypeArguments.Single().IsGenericType &&
            type.GenericTypeArguments.Single().GetGenericTypeDefinition() ==
            context.GetContextType(typeof(KeyValuePair<,>)))
        {
            return new V1JSONSchemaProps
            {
                Type = Object,
                AdditionalProperties = context.Map(type.GenericTypeArguments.Single().GenericTypeArguments[1]),
            };
        }

        if (!IsSimpleType(type)
            && type.IsGenericType
            && type.GetGenericTypeDefinition() == context.GetContextType(typeof(IDictionary<,>)))
        {
            return new V1JSONSchemaProps
            {
                Type = Object, AdditionalProperties = context.Map(type.GenericTypeArguments[1]),
            };
        }

        if (!IsSimpleType(type) &&
            (context.GetContextType<IDictionary>().IsAssignableFrom(type) ||
             (type.IsGenericType &&
              type.GetGenericArguments().FirstOrDefault()?.IsGenericType == true &&
              type.GetGenericArguments().FirstOrDefault()?.GetGenericTypeDefinition() ==
              context.GetContextType(typeof(KeyValuePair<,>)))))
        {
            return new V1JSONSchemaProps { Type = Object, XKubernetesPreserveUnknownFields = true };
        }

        if (!IsSimpleType(type) && IsGenericEnumerableType(type, out Type? closingType))
        {
            return new V1JSONSchemaProps { Type = Array, Items = context.Map(closingType), };
        }

        if (type == context.GetContextType<IntstrIntOrString>())
        {
            return new V1JSONSchemaProps { XKubernetesIntOrString = true };
        }

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
            };
        }

        if (type == context.GetContextType<int>() ||
            type == context.GetContextType<int?>())
        {
            return new V1JSONSchemaProps { Type = Integer, Format = Int32 };
        }

        if (type == context.GetContextType<long>() ||
            type == context.GetContextType<long?>())
        {
            return new V1JSONSchemaProps { Type = Integer, Format = Int64 };
        }

        if (type == context.GetContextType<float>() ||
            type == context.GetContextType<float?>())
        {
            return new V1JSONSchemaProps { Type = Number, Format = Float };
        }

        if (type == context.GetContextType<double>() ||
            type == context.GetContextType<double?>())
        {
            return new V1JSONSchemaProps { Type = Number, Format = Double };
        }

        if (type == context.GetContextType<string>() ||
            type == context.GetContextType<string?>())
        {
            return new V1JSONSchemaProps { Type = String };
        }

        if (type == context.GetContextType<bool>() ||
            type == context.GetContextType<bool?>())
        {
            return new V1JSONSchemaProps { Type = Boolean };
        }

        if (type == context.GetContextType<DateTime>() ||
            type == context.GetContextType<DateTime?>())
        {
            return new V1JSONSchemaProps { Type = String, Format = DateTime };
        }

        if (type.IsEnum)
        {
            return new V1JSONSchemaProps { Type = String, EnumProperty = Enum.GetNames(type).Cast<object>().ToList() };
        }

        if (type.IsGenericType && type.FullName?.Contains("Nullable") == true && type.GetGenericArguments()[0].IsEnum)
        {
            return new V1JSONSchemaProps
            {
                Type = String, EnumProperty = Enum.GetNames(type.GetGenericArguments()[0]).Cast<object>().ToList(),
            };
        }

        if (!IsSimpleType(type))
        {
            return new V1JSONSchemaProps
            {
                Type = Object,
                Description =
                    type.GetCustomAttributeData<DescriptionAttribute>()?.GetCustomAttributeCtorArg<string>(context, 0),
                Properties = type
                    .GetProperties()
                    .Where(p => p.GetCustomAttributeData<IgnoreAttribute>() == null)
                    .Select(p => (Name: p.GetPropertyName(context), Schema: context.Map(p)))
                    .ToDictionary(t => t.Name, t => t.Schema),
                Required = type.GetProperties()
                        .Where(p => p.GetCustomAttributeData<RequiredAttribute>() != null)
                        .Where(p => p.GetCustomAttributeData<IgnoreAttribute>() == null)
                        .Select(p => p.GetPropertyName(context))
                        .ToList() switch
                    {
                        { Count: > 0 } p => p,
                        _ => null,
                    },
            };
        }

        throw new ArgumentException($"The given type {type.FullName} is not a valid Kubernetes entity.");

        bool IsSimpleType(Type t) =>
            t.IsPrimitive ||
            new[]
            {
                context.GetContextType<string>(), context.GetContextType<decimal>(),
                context.GetContextType<DateTime>(), context.GetContextType<DateTimeOffset>(),
                context.GetContextType<TimeSpan>(), context.GetContextType<Guid>(),
            }.Contains(t) ||
            t.IsEnum ||
            Convert.GetTypeCode(t) != TypeCode.Object ||
            (t.IsGenericType &&
             t.GetGenericTypeDefinition() == context.GetContextType(typeof(Nullable<>)) &&
             IsSimpleType(t.GetGenericArguments()[0]));

        bool IsGenericEnumerableType(
            Type theType,
            [NotNullWhen(true)] out Type? enclosingType)
        {
            if (theType.IsGenericType && context.GetContextType(typeof(IEnumerable<>))
                    .IsAssignableFrom(theType.GetGenericTypeDefinition()))
            {
                enclosingType = theType.GetGenericArguments()[0];
                return true;
            }

            enclosingType = theType
                .GetInterfaces()
                .Where(t => t.IsGenericType &&
                            t.GetGenericTypeDefinition() == context.GetContextType(typeof(IEnumerable<>)))
                .Select(t => t.GetGenericArguments()[0])
                .FirstOrDefault();

            return enclosingType != null;
        }
    }
}
