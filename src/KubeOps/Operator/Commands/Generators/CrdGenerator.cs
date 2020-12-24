using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;
using k8s.Versioning;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Serialization;
using KubeOps.Operator.Services;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("crd", "crds", Description = "Generates the needed CRD for kubernetes.")]
    internal class CrdGenerator : GeneratorBase
    {
        private readonly EntitySerializer _serializer;

        private readonly IResourceTypeService _resourceTypeService;

        public CrdGenerator(EntitySerializer serializer, IResourceTypeService resourceTypeService)
        {
            _serializer = serializer;
            _resourceTypeService = resourceTypeService;
        }

        [Option("--use-old-crds", Description = "Defines that the old crd definitions (V1Beta1) should be used.")]
        public bool UseOldCrds { get; set; }

        public IEnumerable<V1CustomResourceDefinition> GenerateCrds()
        {
            var resourceTypes = _resourceTypeService.GetResourceTypesByAttribute<KubernetesEntityAttribute>();

            return resourceTypes
                .Where(type => !type.GetCustomAttributes<IgnoreEntityAttribute>().Any())
                .Select(type => (type.CreateCrd(), type.GetCustomAttributes<StorageVersionAttribute>().Any()))
                .GroupBy(grp => grp.Item1.Metadata.Name)
                .Select(
                    group =>
                    {
                        if (group.Count(def => def.Item2) > 1)
                        {
                            throw new Exception("There are multiple stored versions on an entity.");
                        }

                        var crd = group.First().Item1;
                        crd.Spec.Versions = group
                            .SelectMany(
                                c => c.Item1.Spec.Versions.Select(
                                    v =>
                                    {
                                        v.Served = true;
                                        v.Storage = c.Item2;
                                        return v;
                                    }))
                            .OrderByDescending(v => v.Name, KubernetesVersionComparer.Instance)
                            .ToList();

                        // when only one version exists, or when no StorageVersion attributes are found
                        // the first version in the list is the stored one.
                        if (crd.Spec.Versions.Count == 1 || group.Count(def => def.Item2) == 0)
                        {
                            crd.Spec.Versions[0].Storage = true;
                        }

                        return crd;
                    });
        }

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
                    ? _serializer.Serialize((V1beta1CustomResourceDefinition)crd, Format)
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
    }
}
