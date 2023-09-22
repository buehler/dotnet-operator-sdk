using k8s.Models;

using KubeOps.Operator.Commands.CommandHelpers;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Serialization;

using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators;

[Command("installer", Description = "Generates kustomization YAML for installing the entire operator.")]
internal class InstallerGenerator : OutputBase
{
    private readonly OperatorSettings _settings;

    public InstallerGenerator(OperatorSettings settings) => _settings = settings;

    [Option("--crds-dir", Description = "The path where the CRD YAML files are located.")]
    public string CrdsPath { get; set; } = "../crds";

    [Option("--rbac-dir", Description = "The path where the RBAC YAML files are located.")]
    public string RbacPath { get; set; } = "../rbac";

    [Option("--operator-dir", Description = "The path where the operator YAML files are located.")]
    public string OperatorPath { get; set; } = "../operator";

    [Option("--image-name", Description = "The name of the operator's Docker image.")]
    public string ImageName { get; set; } = "public-docker-image-path";

    [Option("--image-tag", Description = "The tag for the Docker image.")]
    public string ImageTag { get; set; } = "latest";

    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var fileWriter = new FileWriter(app.Out);
        fileWriter.Add(
            $"namespace.{Format.ToString().ToLower()}",
            EntitySerializer.Serialize(
                new V1Namespace(
                    V1Namespace.KubeApiVersion,
                    V1Namespace.KubeKind,
                    new V1ObjectMeta(name: "system")),
                Format));
        fileWriter.Add(
            $"kustomization.{Format.ToString().ToLower()}",
            EntitySerializer.Serialize(
                new KustomizationConfig
                {
                    NamePrefix = $"{_settings.Name}-",
                    Namespace = $"{_settings.Name}-system",
                    CommonLabels = new Dictionary<string, string> { { "operator", _settings.Name }, },
                    Resources = new List<string>
                    {
                        $"./namespace.{Format.ToString().ToLower()}",
                        OutputPath == null
                            ? CrdsPath
                            : Path.GetRelativePath(OutputPath, CrdsPath).Replace('\\', '/'),
                        OutputPath == null
                            ? RbacPath
                            : Path.GetRelativePath(OutputPath, RbacPath).Replace('\\', '/'),
                        OutputPath == null
                            ? OperatorPath
                            : Path.GetRelativePath(OutputPath, OperatorPath).Replace('\\', '/'),
                    },
                    Images = new List<KustomizationImage>
                    {
                        new() { Name = "operator", NewName = ImageName, NewTag = ImageTag, },
                    },
                },
                Format));

        await fileWriter.OutputAsync(OutputPath);
        return ExitCodes.Success;
    }
}
