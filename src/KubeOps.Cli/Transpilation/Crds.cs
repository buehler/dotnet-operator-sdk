using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

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
        var (meta, scope) = Entities.ToEntityMetadata(type);
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
                ShortNames = type
                    .GetCustomAttributeData<KubernetesEntityShortNamesAttribute>()
                    ?.GetCustomAttributeCtorArg<string[]>(0),
            };
        crd.Spec.Scope = scope;

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
            Description = type.GetCustomAttributeData<DescriptionAttribute>()?.GetCustomAttributeCtorArg<string>(0),
            Properties = type.GetProperties()
                .Where(p => !IgnoredToplevelProperties.Contains(p.Name.ToLowerInvariant()))
                .Where(p => p.GetCustomAttributeData<IgnoreAttribute>() == null)
                .Select(p => (Name: p.GetPropertyName(), Schema: context.Map(p)))
                .ToDictionary(t => t.Name, t => t.Schema),
        });

        // version.AdditionalPrinterColumns = MapPrinterColumns(type).ToList() switch
        // {
        //     { Count: > 0 } l => l,
        //     _ => null,
        // };
        crd.Spec.Versions = new List<V1CustomResourceDefinitionVersion> { version };
        crd.Validate();

        return crd;
    }

    public static IEnumerable<V1CustomResourceDefinition> Transpile(
        this MetadataLoadContext context,
        IEnumerable<Type> types)
        => types
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

    private static string GetPropertyName(this PropertyInfo prop)
    {
        var name = prop.GetCustomAttributeData<JsonPropertyNameAttribute>() switch
        {
            null => prop.Name,
            { } attr => attr.GetCustomAttributeCtorArg<string>(0) ?? prop.Name,
        };

        return $"{name[..1].ToLowerInvariant()}{name[1..]}";
    }

    private static V1JSONSchemaProps Map(this MetadataLoadContext context, PropertyInfo prop)
    {
        var props = context.Map(prop.PropertyType);

        // TODO: xml docs
        props.Description ??= prop.GetCustomAttributeData<DescriptionAttribute>()?.GetCustomAttributeCtorArg<string>(0);
        if (string.IsNullOrWhiteSpace(props.Description))
        {
            props.Description = null;
        }

        // TODO nullable

        if (prop.GetCustomAttributeData<ExternalDocsAttribute>() is { } extDocs)
        {
            props.ExternalDocs = new V1ExternalDocumentation(
                extDocs.GetCustomAttributeCtorArg<string>(0),
                extDocs.GetCustomAttributeCtorArg<string>(1));
        }

        if (prop.GetCustomAttributeData<ItemsAttribute>() is { } items)
        {
            props.MinItems = items.GetCustomAttributeNamedArg<long>(nameof(ItemsAttribute.MinItems));
            props.MaxItems = items.GetCustomAttributeNamedArg<long>(nameof(ItemsAttribute.MaxItems));
        }

        if (prop.GetCustomAttributeData<LengthAttribute>() is { } length)
        {
            props.MinLength = length.GetCustomAttributeNamedArg<long>(nameof(LengthAttribute.MinLength));
            props.MaxLength = length.GetCustomAttributeNamedArg<long>(nameof(LengthAttribute.MaxLength));
        }

        if (prop.GetCustomAttributeData<MultipleOfAttribute>() is { } multi)
        {
            props.MultipleOf = multi.GetCustomAttributeNamedArg<double?>(nameof(MultipleOfAttribute.Value));
        }

        if (prop.GetCustomAttributeData<PatternAttribute>() is { } pattern)
        {
            props.Pattern = pattern.GetCustomAttributeNamedArg<string>(nameof(PatternAttribute.RegexPattern));
        }

        if (prop.GetCustomAttributeData<RangeMaximumAttribute>() is { } rangeMax)
        {
            props.Maximum = rangeMax.GetCustomAttributeNamedArg<double>(nameof(RangeMaximumAttribute.Maximum));
            props.ExclusiveMaximum =
                rangeMax.GetCustomAttributeNamedArg<bool>(nameof(RangeMaximumAttribute.ExclusiveMaximum));
        }

        if (prop.GetCustomAttributeData<RangeMinimumAttribute>() is { } rangeMin)
        {
            props.Minimum = rangeMin.GetCustomAttributeNamedArg<double>(nameof(RangeMinimumAttribute.Minimum));
            props.ExclusiveMinimum =
                rangeMin.GetCustomAttributeNamedArg<bool>(nameof(RangeMinimumAttribute.ExclusiveMinimum));
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
            && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
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
            Nullable.GetUnderlyingType(type) == context.GetContextType<int>())
        {
            return new V1JSONSchemaProps { Type = Integer, Format = Int32 };
        }

        if (type == context.GetContextType<long>() ||
            Nullable.GetUnderlyingType(type) == context.GetContextType<long>())
        {
            return new V1JSONSchemaProps { Type = Integer, Format = Int64 };
        }

        if (type == context.GetContextType<float>() ||
            Nullable.GetUnderlyingType(type) == context.GetContextType<float>())
        {
            return new V1JSONSchemaProps { Type = Number, Format = Float };
        }

        if (type == context.GetContextType<double>() ||
            Nullable.GetUnderlyingType(type) == context.GetContextType<double>())
        {
            return new V1JSONSchemaProps { Type = Number, Format = Double };
        }

        if (type == context.GetContextType<string>() ||
            Nullable.GetUnderlyingType(type) == context.GetContextType<string>())
        {
            return new V1JSONSchemaProps { Type = String };
        }

        if (type == context.GetContextType<bool>() ||
            Nullable.GetUnderlyingType(type) == context.GetContextType<bool>())
        {
            return new V1JSONSchemaProps { Type = Boolean };
        }

        if (type == context.GetContextType<DateTime>() ||
            Nullable.GetUnderlyingType(type) == context.GetContextType<DateTime>())
        {
            return new V1JSONSchemaProps { Type = String, Format = DateTime };
        }

        if (type.IsEnum)
        {
            return new V1JSONSchemaProps { Type = String, EnumProperty = Enum.GetNames(type).Cast<object>().ToList() };
        }

        if (Nullable.GetUnderlyingType(type)?.IsEnum == true)
        {
            return new V1JSONSchemaProps
            {
                Type = String,
                EnumProperty = Enum.GetNames(Nullable.GetUnderlyingType(type)!).Cast<object>().ToList(),
            };
        }

        if (!IsSimpleType(type))
        {
            return new V1JSONSchemaProps
            {
                Type = Object,
                Description = type.GetCustomAttributeData<DescriptionAttribute>()?.GetCustomAttributeCtorArg<string>(0),
                Properties = type
                    .GetProperties()
                    .Where(p => p.GetCustomAttributeData<IgnoreAttribute>() == null)
                    .Select(p => (Name: p.GetPropertyName(), Schema: context.Map(p)))
                    .ToDictionary(t => t.Name, t => t.Schema),
                Required = type.GetProperties()
                        .Where(p => p.GetCustomAttributeData<RequiredAttribute>() != null)
                        .Where(p => p.GetCustomAttributeData<IgnoreAttribute>() == null)
                        .Select(p => p.GetPropertyName())
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

    private static Type GetContextType<T>(this MetadataLoadContext context)
        => context.GetContextType(typeof(T));

    private static Type GetContextType(this MetadataLoadContext context, Type type)
    {
        foreach (var assembly in context.GetAssemblies())
        {
            if (assembly.GetType(type.FullName!) is { } t)
            {
                return t;
            }
        }

        var newAssembly = context.LoadFromAssemblyPath(type.Assembly.Location);
        return newAssembly.GetType(type.FullName!)!;
    }
}
