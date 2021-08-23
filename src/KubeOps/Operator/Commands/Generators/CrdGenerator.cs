using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Commands.CommandHelpers;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("crd", "crds", Description = "Generates the needed CRD for kubernetes.")]
    internal class CrdGenerator : GeneratorBase
    {
        private readonly EntitySerializer _serializer;
        private readonly ICrdBuilder _crdBuilder;

        public CrdGenerator(EntitySerializer serializer, ICrdBuilder crdBuilder)
        {
            _serializer = serializer;
            _crdBuilder = crdBuilder;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var crds = _crdBuilder.BuildCrds().ToList();

            var fileWriter = new FileWriter(app.Out);
            foreach (var crd in crds)
            {
                fileWriter.Add(
                    $"{crd.Metadata.Name.Replace('.', '_')}.{Format.ToString().ToLower()}",
                    _serializer.Serialize(crd, Format));
            }

            fileWriter.Add(
                $"kustomization.{Format.ToString().ToLower()}",
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

            await fileWriter.OutputAsync(OutputPath);
            return ExitCodes.Success;
        }
    }
}
