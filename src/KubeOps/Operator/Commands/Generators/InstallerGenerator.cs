using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("installer", Description = "Generates kustomization yaml for the whole installation of the operator.")]
    internal class InstallerGenerator : GeneratorBase
    {
        private readonly EntitySerializer _serializer;
        private readonly OperatorSettings _settings;

        public InstallerGenerator(EntitySerializer serializer, OperatorSettings settings)
        {
            _serializer = serializer;
            _settings = settings;
        }

        [Option("--crds-dir", Description = "The path where the crds are located.")]
        public string? CrdsPath { get; set; }

        [Option("--rbac-dir", Description = "The path where the rbac yamls are located.")]
        public string? RbacPath { get; set; }

        [Option("--operator-dir", Description = "The path where the operator yamls are located.")]
        public string? OperatorPath { get; set; }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var ns = _serializer.Serialize(new V1Namespace(
                V1Namespace.KubeApiVersion,
                V1Namespace.KubeKind,
                new V1ObjectMeta(name: "system")), Format);
            var kustomize = _serializer.Serialize(new KustomizationConfig
            {
                NamePrefix = $"{_settings.Name}-",
                Namespace = $"{_settings.Name}-system",
                CommonLabels = new Dictionary<string, string>
                {
                    {"operator", _settings.Name},
                },
                Resources = new List<string>
                {
                    $"./namespace.{Format.ToString().ToLower()}",
                    CrdsPath == null || OutputPath == null
                        ? "../crds"
                        : Path.GetRelativePath(OutputPath, CrdsPath).Replace('\\', '/'),
                    RbacPath == null || OutputPath == null
                        ? "../rbac"
                        : Path.GetRelativePath(OutputPath, RbacPath).Replace('\\', '/'),
                    OperatorPath == null || OutputPath == null
                        ? "../operator"
                        : Path.GetRelativePath(OutputPath, OperatorPath).Replace('\\', '/'),
                },
                Images = new List<KustomizationImage>
                {
                    new KustomizationImage
                    {
                        Name = "operator",
                        NewName = "public-docker-image-path",
                        NewTag = "latest",
                    },
                },
            }, Format);

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
                await using var nsFile = File.Open(Path.Join(OutputPath,
                    $"namespace.{Format.ToString().ToLower()}"), FileMode.Create);
                await nsFile.WriteAsync(Encoding.UTF8.GetBytes(ns));
                await using var kustomizeFile = File.Open(Path.Join(OutputPath,
                    $"kustomization.{Format.ToString().ToLower()}"), FileMode.Create);
                await kustomizeFile.WriteAsync(Encoding.UTF8.GetBytes(kustomize));
            }
            else
            {
                await app.Out.WriteLineAsync(ns);
                await app.Out.WriteLineAsync(kustomize);
            }

            return ExitCodes.Success;
        }
    }
}
