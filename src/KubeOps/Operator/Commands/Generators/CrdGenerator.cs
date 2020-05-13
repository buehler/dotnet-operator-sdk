using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("crd", "crds", Description = "Generates the needed CRD for kubernetes.")]
    internal class CrdGenerator : GeneratorBase
    {
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

                var kustomizeOutput = Encoding.UTF8.GetBytes(
                    _serializer.Serialize(
                        new KustomizationConfig
                        {
                            Resources = crds
                                .Select(crd => $"{crd.Metadata.Name.Replace('.', '_')}.{Format.ToString().ToLower()}")
                                .ToList(),
                            CommonLabels = new Dictionary<string, string>
                            {
                                { "operator-element", "crd" },
                            },
                        },
                        Format));
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
                    await using var file = File.Open(
                        Path.Join(
                            OutputPath,
                            $"{crd.Metadata.Name.Replace('.', '_')}.{Format.ToString().ToLower()}"),
                        FileMode.Create);
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

            return GetTypesWithAttribute<KubernetesEntityAttribute>(assembly)
                .Select(EntityToCrdExtensions.CreateCrd);
        }

        private static IEnumerable<Type> GetTypesWithAttribute<TAttribute>(Assembly assembly)
            where TAttribute : Attribute =>
            assembly.GetTypes().Where(type => type.GetCustomAttributes<TAttribute>().Any());
    }
}
