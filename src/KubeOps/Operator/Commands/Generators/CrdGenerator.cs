using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.KubernetesEntities;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;
using V1JSONSchemaProps = k8s.Models.V1JSONSchemaProps;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("crd", "crds", Description = "Generates the needed CRD for kubernetes.")]
    internal class CrdGenerator : GeneratorBase
    {
        private const string Integer = "integer";
        private const string Number = "number";
        private const string String = "string";
        private const string Boolean = "boolean";
        private const string Object = "object";

        private const string Int32 = "int32";
        private const string Int64 = "int64";
        private const string Float = "float";
        private const string Double = "double";
        private const string DateTime = "date-time";

        private readonly EntitySerializer _serializer;

        public CrdGenerator(EntitySerializer serializer)
        {
            _serializer = serializer;
        }

        [Option("--use-old-crds", Description = "Defines that the old crd definitions (V1Beta1) should be used.")]
        public bool UseOldCrds { get; set; }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var crds = GenerateCrds().ToList();
            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);

                var kustomizeOutput = Encoding.UTF8.GetBytes(_serializer.Serialize(new KustomizationConfig
                {
                    Resources = crds
                        .Select(crd => $"{crd.Metadata.Name.Replace('.', '_')}.{Format.ToString().ToLower()}").ToList(),
                    CommonLabels = new Dictionary<string, string>
                    {
                        {"operator-element", "crd"},
                    },
                }, Format));
                await using var kustomizationFile =
                    File.Open(Path.Join(OutputPath, $"kustomization.{Format.ToString().ToLower()}"), FileMode.Create);
                await kustomizationFile.WriteAsync(kustomizeOutput);
            }

            foreach (var crd in crds)
            {
                var output = UseOldCrds
                    ? _serializer.Serialize(crd.Convert(), Format)
                    : _serializer.Serialize(crd, Format);

                if (!string.IsNullOrWhiteSpace(OutputPath))
                {
                    await using var file = File.Open(Path.Join(OutputPath,
                        $"{crd.Metadata.Name.Replace('.', '_')}.{Format.ToString().ToLower()}"), FileMode.Create);
                    await file.WriteAsync(Encoding.UTF8.GetBytes(output));
                }
                else
                {
                    await app.Out.WriteLineAsync(output);
                }
            }

            return ExitCodes.Success;
        }

        public static IEnumerable<V1CustomResourceDefinition> GenerateCrds()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
            {
                throw new Exception("No Entry Assembly found.");
            }

            var result = new List<V1CustomResourceDefinition>();
            foreach (var entityType in GetTypesWithAttribute<KubernetesEntityAttribute>(assembly))
            {
                var entityDefinition = EntityExtensions.CreateResourceDefinition(entityType);

                var crd = new V1CustomResourceDefinition(
                    new V1CustomResourceDefinitionSpec(),
                    $"{V1CustomResourceDefinition.KubeGroup}/{V1CustomResourceDefinition.KubeApiVersion}",
                    V1CustomResourceDefinition.KubeKind,
                    new V1ObjectMeta {Name = $"{entityDefinition.Plural}.{entityDefinition.Group}"});

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
                spec.Versions = new[] {version};

                version.Name = entityDefinition.Version;
                version.Served = true;
                version.Storage = true;

                if (entityType.GetProperty("Status") != null)
                {
                    version.Subresources = new V1CustomResourceSubresources(null, new { });
                }

                version.Schema = new V1CustomResourceValidation(MapType(entityType));

                result.Add(crd);
            }

            return result;
        }

        private static V1JSONSchemaProps MapProperty(PropertyInfo info)
        {
            // TODO: get description somehow.
            // TODO: support description via XML fields -> but describe how (generate xml file stuff)

            var props = MapType(info.PropertyType);
            props.Description ??= info.GetCustomAttribute<DisplayAttribute>()?.Description;
            return props;
        }

        private static V1JSONSchemaProps MapType(Type type)
        {
            var props = new KubernetesEntities.V1JSONSchemaProps();

            // this description is on the class
            props.Description = type.GetCustomAttributes<DisplayAttribute>(true).FirstOrDefault()?.Description;

            // TODO: validator attributes

            if (type == typeof(V1ObjectMeta))
            {
                // TODO(check): is this correct? should metadata be filtered?
                props.Type = Object;
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

            if (Nullable.GetUnderlyingType(type) != null)
            {
                props.Nullable = true;
            }

            return props;
        }

        private static IEnumerable<Type> GetTypesWithAttribute<TAttribute>(Assembly assembly)
            where TAttribute : Attribute =>
            assembly.GetTypes().Where(type => type.GetCustomAttributes<TAttribute>().Any());

        private static bool IsSimpleType(Type type) =>
            type.IsPrimitive ||
            new[]
            {
                typeof(string),
                typeof(decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Guid)
            }.Contains(type) ||
            type.IsEnum ||
            Convert.GetTypeCode(type) != TypeCode.Object ||
            (type.IsGenericType &&
             type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
             IsSimpleType(type.GetGenericArguments()[0]));

        private static string CamelCase(string str) => $"{str.Substring(0, 1).ToLower()}{str.Substring(1)}";
    }
}
