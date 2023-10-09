using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

using Namotion.Reflection;

namespace KubeOps.Transpiler;

/// <summary>
/// Class for the conversion of C# types to Kubernetes CRDs.
/// </summary>
public static partial class Crds
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

    /// <summary>
    /// Transpiles the given Kubernetes entity type to a <see cref="V1CustomResourceDefinition"/> object.
    /// </summary>
    /// <param name="type">The Kubernetes entity type to transpile.</param>
    /// <returns>A <see cref="V1CustomResourceDefinition"/> object representing the transpiled entity type.</returns>
    /// <exception cref="ArgumentException">Thrown if the given type is not a valid Kubernetes entity.</exception>
    public static V1CustomResourceDefinition Transpile(Type type)
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
                ShortNames = type.GetCustomAttributes<KubernetesEntityShortNamesAttribute>(true)
                        .SelectMany(a => a.ShortNames).ToList() switch
                {
                    { Count: > 0 } p => p,
                    _ => null,
                },
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
            Description = type.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault() switch
            {
                { } attr => attr.Description,
                _ => null,
            },
            Properties = type.GetProperties()
                .Where(p => !IgnoredToplevelProperties.Contains(p.Name.ToLowerInvariant()))
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                .Select(p => (Name: TypeMapping.PropertyName(p), Schema: MapProperty(p)))
                .ToDictionary(t => t.Name, t => t.Schema),
        });

        version.AdditionalPrinterColumns = MapPrinterColumns(type).ToList() switch
        {
            { Count: > 0 } l => l,
            _ => null,
        };
        crd.Spec.Versions = new List<V1CustomResourceDefinitionVersion> { version };
        crd.Validate();

        return crd;
    }

    /// <summary>
    /// Transpiles the given sequence of Kubernetes entity types to a
    /// sequence of <see cref="V1CustomResourceDefinition"/> objects.
    /// The definitions are grouped by version and one stored version is defined.
    /// The transpiler fails when multiple stored versions are defined.
    /// </summary>
    /// <param name="types">The sequence of Kubernetes entity types to transpile.</param>
    /// <returns>A sequence of <see cref="V1CustomResourceDefinition"/> objects representing the transpiled entity types.</returns>
    public static IEnumerable<V1CustomResourceDefinition> Transpile(IEnumerable<Type> types)
        => types
            .Where(type => type.Assembly != typeof(KubernetesEntityAttribute).Assembly)
            .Where(type => type.GetCustomAttributes<KubernetesEntityAttribute>().Any())
            .Where(type => !type.GetCustomAttributes<IgnoreAttribute>().Any())
            .Select(type => (Props: Transpile(type),
                IsStorage: type.GetCustomAttributes<StorageVersionAttribute>().Any()))
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

    private static IEnumerable<V1CustomResourceColumnDefinition> MapPrinterColumns(Type type)
    {
        var props = type.GetProperties().Select(p => (Prop: p, Path: string.Empty)).ToList();
        while (props.Count > 0)
        {
            var (prop, path) = props[0];
            props.RemoveAt(0);

            if (prop.PropertyType.IsClass)
            {
                props.AddRange(prop.PropertyType.GetProperties()
                    .Select(p => (Prop: p, Path: $"{path}.{TypeMapping.PropertyName(prop)}")));
            }

            if (prop.GetCustomAttribute<AdditionalPrinterColumnAttribute>() is not { } attr)
            {
                continue;
            }

            var mapped = MapProperty(prop);
            yield return new V1CustomResourceColumnDefinition
            {
                Name = attr.Name ?? TypeMapping.PropertyName(prop),
                JsonPath = $"{path}.{TypeMapping.PropertyName(prop)}",
                Type = mapped.Type,
                Description = mapped.Description,
                Format = mapped.Format,
                Priority = attr.Priority switch
                {
                    PrinterColumnPriority.StandardView => 0,
                    _ => 1,
                },
            };
        }

        foreach (var attr in type.GetCustomAttributes<GenericAdditionalPrinterColumnAttribute>(true))
        {
            yield return new V1CustomResourceColumnDefinition
            {
                Name = attr.Name,
                JsonPath = attr.JsonPath,
                Type = attr.Type,
                Description = attr.Description,
                Format = attr.Format,
                Priority = attr.Priority switch
                {
                    PrinterColumnPriority.StandardView => 0,
                    _ => 1,
                },
            };
        }
    }

    private static V1JSONSchemaProps MapProperty(PropertyInfo prop)
    {
        var props = TypeMapping.Map(prop.PropertyType);
        var ctx = prop.ToContextualProperty();
        props.Description ??= prop.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault()?.Description ??
                              ctx.GetXmlDocsSummary();
        if (string.IsNullOrWhiteSpace(props.Description))
        {
            props.Description = null;
        }

        if (ctx.Nullability == Nullability.Nullable)
        {
            props.Nullable = true;
        }

        return AttributeMapping.Map(prop, props);
    }

    private static class AttributeMapping
    {
        private static readonly Func<PropertyInfo, V1JSONSchemaProps, V1JSONSchemaProps>[] Mappers =
        {
            Mapper<ExternalDocsAttribute>((a, p) =>
                p.ExternalDocs = new V1ExternalDocumentation(a.Description, a.Url)),
            Mapper<ItemsAttribute>((a, p) =>
            {
                p.MinItems = a.MinItems;
                p.MaxItems = a.MaxItems;
            }),
            Mapper<LengthAttribute>((a, p) =>
            {
                p.MinLength = a.MinLength;
                p.MaxLength = a.MaxLength;
            }),
            Mapper<MultipleOfAttribute>((a, p) => p.MultipleOf = a.Value),
            Mapper<PatternAttribute>((a, p) => p.Pattern = a.RegexPattern), Mapper<RangeMaximumAttribute>((a, p) =>
            {
                p.Maximum = a.Maximum;
                p.ExclusiveMaximum = a.ExclusiveMaximum;
            }),
            Mapper<RangeMinimumAttribute>((a, p) =>
            {
                p.Minimum = a.Minimum;
                p.ExclusiveMinimum = a.ExclusiveMinimum;
            }),
            Mapper<PreserveUnknownFieldsAttribute>((_, p) => p.XKubernetesPreserveUnknownFields = true),
            Mapper<EmbeddedResourceAttribute>((_, p) =>
            {
                p.XKubernetesEmbeddedResource = true;
                p.XKubernetesPreserveUnknownFields = true;
                p.Type = Object;
                p.Properties = null;
            }),
        };

        public static V1JSONSchemaProps Map(PropertyInfo prop, V1JSONSchemaProps props) =>
            Mappers.Aggregate(props, (current, mapper) => mapper(prop, current));

        private static Func<PropertyInfo, V1JSONSchemaProps, V1JSONSchemaProps> Mapper<TAttribute>(
            Action<TAttribute, V1JSONSchemaProps> attributeMapper)
            where TAttribute : Attribute
            => (prop, props) =>
            {
                if (prop.GetCustomAttribute<TAttribute>() is { } attr)
                {
                    attributeMapper(attr, props);
                }

                return props;
            };
    }

    private static class TypeMapping
    {
        private static readonly Func<Type, Func<Type, V1JSONSchemaProps?>, V1JSONSchemaProps?>[] Mappers =
        {
            Mapper(MapV1ObjectMeta), MapArray, Mapper(MapResourceQuantityDict), MapDictionary,
            MapGenericObjectEnumerable, Mapper(MapArbitraryDictionary), MapGenericEnumerable,
            Mapper(MapIntOrString), Mapper(MapKubernetesObject), Mapper(MapInt), Mapper(MapLong), Mapper(MapFloat),
            Mapper(MapDouble), Mapper(MapString), Mapper(MapBool), Mapper(MapDateTime), Mapper(MapEnum),
            Mapper(MapNullableEnum), Mapper(MapComplexType),
        };

        public static V1JSONSchemaProps Map(Type type)
        {
            foreach (var mapper in Mappers)
            {
                var mapped = mapper(type, Map);
                if (mapped != null)
                {
                    return mapped;
                }
            }

            throw new ArgumentException($"The given type {type.FullName} is not a valid Kubernetes entity.");
        }

        public static string PropertyName(PropertyInfo prop)
        {
            var name = prop.GetCustomAttribute<JsonPropertyNameAttribute>() switch
            {
                null => prop.Name,
                { Name: { } attrName } => attrName,
            };

            return $"{name[..1].ToLowerInvariant()}{name[1..]}";
        }

        private static Func<Type, Func<Type, V1JSONSchemaProps?>, V1JSONSchemaProps?> Mapper(
            Func<Type, V1JSONSchemaProps?> mapper)
            => (t, _) => mapper(t);

        private static V1JSONSchemaProps? MapV1ObjectMeta(Type type)
            => type == typeof(V1ObjectMeta)
                ? new V1JSONSchemaProps { Type = Object }
                : null;

        private static V1JSONSchemaProps? MapArray(Type type, Func<Type, V1JSONSchemaProps?> map)
            => type.IsArray && type.GetElementType() != null
                ? new V1JSONSchemaProps { Type = Array, Items = map(type.GetElementType()!) }
                : null;

        private static V1JSONSchemaProps? MapResourceQuantityDict(Type type)
            => !IsSimpleType(type)
               && type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
               && type.GenericTypeArguments.Contains(typeof(ResourceQuantity))
                ? new V1JSONSchemaProps { Type = Object, XKubernetesPreserveUnknownFields = true }
                : null;

        private static V1JSONSchemaProps? MapDictionary(Type type, Func<Type, V1JSONSchemaProps?> map)
            => !IsSimpleType(type)
               && type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                ? new V1JSONSchemaProps { Type = Object, AdditionalProperties = map(type.GenericTypeArguments[1]) }
                : null;

        private static V1JSONSchemaProps? MapGenericObjectEnumerable(Type type, Func<Type, V1JSONSchemaProps?> map)
            => !IsSimpleType(type) &&
               type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
               type.GenericTypeArguments.Length == 1 &&
               type.GenericTypeArguments.Single().IsGenericType &&
               type.GenericTypeArguments.Single().GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                ? new V1JSONSchemaProps
                {
                    Type = Object,
                    AdditionalProperties = map(type.GenericTypeArguments.Single().GenericTypeArguments[1]),
                }
                : null;

        private static V1JSONSchemaProps? MapArbitraryDictionary(Type type)
            => !IsSimpleType(type) &&
               (typeof(IDictionary).IsAssignableFrom(type) ||
                (type.IsGenericType &&
                 type.GetGenericArguments().FirstOrDefault()?.IsGenericType == true &&
                 type.GetGenericArguments().FirstOrDefault()?.GetGenericTypeDefinition() ==
                 typeof(KeyValuePair<,>)))
                ? new V1JSONSchemaProps { Type = Object, XKubernetesPreserveUnknownFields = true }
                : null;

        private static V1JSONSchemaProps? MapGenericEnumerable(Type type, Func<Type, V1JSONSchemaProps?> map)
            => !IsSimpleType(type) && IsGenericEnumerableType(type, out Type? closingType)
                ? new V1JSONSchemaProps { Type = Array, Items = map(closingType!), }
                : null;

        private static V1JSONSchemaProps? MapIntOrString(Type type)
            => type == typeof(IntstrIntOrString)
                ? new V1JSONSchemaProps { XKubernetesIntOrString = true }
                : null;

        private static V1JSONSchemaProps? MapKubernetesObject(Type type)
            => typeof(IKubernetesObject).IsAssignableFrom(type) &&
               type is { IsAbstract: false, IsInterface: false } &&
               type.Assembly == typeof(IKubernetesObject).Assembly
                ? new V1JSONSchemaProps
                {
                    Type = Object,
                    Properties = null,
                    XKubernetesPreserveUnknownFields = true,
                    XKubernetesEmbeddedResource = true,
                }
                : null;

        private static V1JSONSchemaProps? MapInt(Type type)
            => type == typeof(int) || type == typeof(int?) || Nullable.GetUnderlyingType(type) == typeof(int)
                ? new V1JSONSchemaProps { Type = Integer, Format = Int32 }
                : null;

        private static V1JSONSchemaProps? MapLong(Type type)
            => type == typeof(long) || type == typeof(long?) || Nullable.GetUnderlyingType(type) == typeof(long)
                ? new V1JSONSchemaProps { Type = Integer, Format = Int64 }
                : null;

        private static V1JSONSchemaProps? MapFloat(Type type)
            => type == typeof(float) || type == typeof(float?) || Nullable.GetUnderlyingType(type) == typeof(float)
                ? new V1JSONSchemaProps { Type = Number, Format = Float }
                : null;

        private static V1JSONSchemaProps? MapDouble(Type type)
            => type == typeof(double) || type == typeof(double?) || Nullable.GetUnderlyingType(type) == typeof(double)
                ? new V1JSONSchemaProps { Type = Number, Format = Double }
                : null;

        private static V1JSONSchemaProps? MapString(Type type)
            => type == typeof(string) || Nullable.GetUnderlyingType(type) == typeof(string)
                ? new V1JSONSchemaProps { Type = String }
                : null;

        private static V1JSONSchemaProps? MapBool(Type type)
            => type == typeof(bool) || type == typeof(bool?) || Nullable.GetUnderlyingType(type) == typeof(bool)
                ? new V1JSONSchemaProps { Type = Boolean }
                : null;

        private static V1JSONSchemaProps? MapDateTime(Type type)
            => type == typeof(DateTime) || type == typeof(DateTime?) ||
               Nullable.GetUnderlyingType(type) == typeof(DateTime)
                ? new V1JSONSchemaProps { Type = String, Format = DateTime }
                : null;

        private static V1JSONSchemaProps? MapEnum(Type type)
            => type.IsEnum
                ? new V1JSONSchemaProps { Type = String, EnumProperty = Enum.GetNames(type).Cast<object>().ToList() }
                : null;

        private static V1JSONSchemaProps? MapNullableEnum(Type type)
            => Nullable.GetUnderlyingType(type)?.IsEnum == true
                ? new V1JSONSchemaProps
                {
                    Type = String,
                    EnumProperty = Enum.GetNames(Nullable.GetUnderlyingType(type)!).Cast<object>().ToList(),
                }
                : null;

        private static V1JSONSchemaProps? MapComplexType(Type type)
            => !IsSimpleType(type)
                ? new V1JSONSchemaProps
                {
                    Type = Object,
                    Description = type.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    Properties = type
                        .GetProperties()
                        .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                        .Select(p => (Name: PropertyName(p), Schema: MapProperty(p)))
                        .ToDictionary(t => t.Name, t => t.Schema),
                    Required = type.GetProperties()
                            .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null)
                            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                            .Select(PropertyName)
                            .ToList() switch
                    {
                        { Count: > 0 } p => p,
                        _ => null,
                    },
                }
                : null;

        private static bool IsSimpleType(Type type) =>
            type.IsPrimitive ||
            new[]
            {
                typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
                typeof(Guid),
            }.Contains(type) ||
            type.IsEnum ||
            Convert.GetTypeCode(type) != TypeCode.Object ||
            (type.IsGenericType &&
             type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
             IsSimpleType(type.GetGenericArguments()[0]));

        private static bool IsGenericEnumerableType(
            Type type,
            [NotNullWhen(true)]
            out Type? closingType)
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

    private sealed partial class KubernetesVersionComparer : IComparer<string>
    {
#if !NET7_0_OR_GREATER
        private static readonly Regex KubernetesVersionRegex =
            new("^v(?<major>[0-9]+)((?<stream>alpha|beta)(?<minor>[0-9]+))?$", RegexOptions.Compiled);
#endif

        private enum Stream
        {
            Alpha = 1,
            Beta = 2,
            Final = 3,
        }

        public int Compare(string? x, string? y)
        {
            if (x == null || y == null)
            {
                return StringComparer.CurrentCulture.Compare(x, y);
            }

#if NET7_0_OR_GREATER
            var matchX = KubernetesVersionRegex().Match(x);
#else
            var matchX = KubernetesVersionRegex.Match(x);
#endif
            if (!matchX.Success)
            {
                return StringComparer.CurrentCulture.Compare(x, y);
            }

#if NET7_0_OR_GREATER
            var matchY = KubernetesVersionRegex().Match(y);
#else
            var matchY = KubernetesVersionRegex.Match(y);
#endif
            if (!matchY.Success)
            {
                return StringComparer.CurrentCulture.Compare(x, y);
            }

            var versionX = ExtractVersion(matchX);
            var versionY = ExtractVersion(matchY);
            return versionX.CompareTo(versionY);
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex("^v(?<major>[0-9]+)((?<stream>alpha|beta)(?<minor>[0-9]+))?$", RegexOptions.Compiled)]
        private static partial Regex KubernetesVersionRegex();
#endif

        private Version ExtractVersion(Match match)
        {
            var major = int.Parse(match.Groups["major"].Value);
            if (!Enum.TryParse<Stream>(match.Groups["stream"].Value, true, out var stream))
            {
                stream = Stream.Final;
            }

            _ = int.TryParse(match.Groups["minor"].Value, out var minor);
            return new Version(major, (int)stream, minor);
        }
    }
}
