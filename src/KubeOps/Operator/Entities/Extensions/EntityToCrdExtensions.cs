using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using k8s;
using k8s.Models;
using KubeOps.Operator.Entities.Annotations;
using Namotion.Reflection;

namespace KubeOps.Operator.Entities.Extensions
{
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

        internal static V1CustomResourceDefinition CreateCrd(
            this IKubernetesObject<V1ObjectMeta> kubernetesEntity) => CreateCrd(kubernetesEntity.GetType());

        internal static V1CustomResourceDefinition CreateCrd<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta> => CreateCrd(typeof(TEntity));

        internal static V1CustomResourceDefinition CreateCrd(this Type entityType)
        {
            var entityDefinition = entityType.CreateResourceDefinition();

            var crd = new V1CustomResourceDefinition(
                new V1CustomResourceDefinitionSpec(),
                $"{V1CustomResourceDefinition.KubeGroup}/{V1CustomResourceDefinition.KubeApiVersion}",
                V1CustomResourceDefinition.KubeKind,
                new V1ObjectMeta { Name = $"{entityDefinition.Plural}.{entityDefinition.Group}" });

            var spec = crd.Spec;
            spec.Group = entityDefinition.Group;
            spec.Names = new V1CustomResourceDefinitionNames
            {
                Kind = entityDefinition.Kind,
                ListKind = entityDefinition.ListKind,
                Singular = entityDefinition.Singular,
                Plural = entityDefinition.Plural,
            };
            spec.Scope = entityDefinition.Scope.ToString();

            var version = new V1CustomResourceDefinitionVersion();
            spec.Versions = new[] { version };

            // TODO: versions?
            version.Name = entityDefinition.Version;
            version.Served = true;
            version.Storage = true;

            if (entityType.GetProperty("Status") != null || entityType.GetProperty("status") != null)
            {
                version.Subresources = new V1CustomResourceSubresources(null, new object());
            }

            version.Schema = new V1CustomResourceValidation(MapType(entityType));

            return crd;
        }

        private static V1JSONSchemaProps MapProperty(PropertyInfo info)
        {
            var props = MapType(info.PropertyType);
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
            if (items != null && items.MaxItems != -1)
            {
                props.MaxItems = items.MaxItems;
            }

            if (items != null && items.MinItems != -1)
            {
                props.MinItems = items.MinItems;
            }

            // Get length description
            var length = info.GetCustomAttribute<LengthAttribute>();
            if (length != null && length.MaxLength != -1)
            {
                props.MaxLength = length.MaxLength;
            }

            if (length != null && length.MinLength != -1)
            {
                props.MinLength = length.MinLength;
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

            if (info.GetCustomAttribute<PreserveUnknownFieldsAttribute>() != null)
            {
                props.XKubernetesPreserveUnknownFields = true;
            }

            if (info.GetCustomAttribute<EmbeddedResourceAttribute>() != null)
            {
                props.Type = null;
                props.Properties = null;
                props.XKubernetesPreserveUnknownFields = true;
                props.XKubernetesEmbeddedResource = true;
            }

            return props;
        }

        private static V1JSONSchemaProps MapType(Type type)
        {
            var props = new V1JSONSchemaProps();

            // this description is on the class
            props.Description ??= type.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault()?.Description;

            if (type == typeof(V1ObjectMeta))
            {
                props.Type = Object;
            }
            else if (type.IsArray)
            {
                props.Type = Array;
                props.Items = MapType(
                    type.GetElementType() ?? throw new NullReferenceException("No Array Element Type found"));
            }
            else if (!IsSimpleType(type) &&
                     (typeof(IDictionary).IsAssignableFrom(type) ||
                      (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
                      (type.IsGenericType &&
                       type.GetGenericArguments().FirstOrDefault()?.GetGenericTypeDefinition() ==
                       typeof(KeyValuePair<,>))))
            {
                props.Type = Object;
                props.XKubernetesPreserveUnknownFields = true;
            }
            else if (type == typeof(IntstrIntOrString))
            {
                props.XKubernetesIntOrString = true;
            }
            else if (!IsSimpleType(type))
            {
                props.Type = Object;

                props.Properties = new Dictionary<string, V1JSONSchemaProps>(
                    type.GetProperties()
                        .Select(
                            prop => KeyValuePair.Create(
                                CamelCase(prop.Name),
                                MapProperty(prop))));
                props.Required = type.GetProperties()
                    .Where(prop => prop.GetCustomAttribute<RequiredAttribute>() != null)
                    .Select(prop => CamelCase(prop.Name))
                    .ToList();
                if (props.Required.Count == 0)
                {
                    props.Required = null;
                }
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

            return props;
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

        private static string CamelCase(string str) => $"{str.Substring(0, 1).ToLower()}{str.Substring(1)}";
    }
}
