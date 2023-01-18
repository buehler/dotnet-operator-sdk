using k8s.Models;
using KubeOps.Operator.Commands.CommandHelpers;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators;

[Command("rbac", Description = "Generates the needed rbac roles for the operator.")]
internal class RbacGenerator : GeneratorBase
{
    private readonly IRbacBuilder _rbacBuilder;

    public RbacGenerator(
        IRbacBuilder rbacBuilder) =>
        _rbacBuilder = rbacBuilder;

    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var fileWriter = new FileWriter(app.Out);
        fileWriter.Add(
            $"operator-role.{Format.ToString().ToLower()}",
            EntitySerializer.Serialize(_rbacBuilder.BuildManagerRbac(), Format));
        fileWriter.Add(
            $"operator-role-binding.{Format.ToString().ToLower()}",
            EntitySerializer.Serialize(
                new V1ClusterRoleBinding
                {
                    ApiVersion = $"{V1ClusterRoleBinding.KubeGroup}/{V1ClusterRoleBinding.KubeApiVersion}",
                    Kind = V1ClusterRoleBinding.KubeKind,
                    Metadata = new V1ObjectMeta { Name = "operator-role-binding" },
                    RoleRef = new V1RoleRef(V1ClusterRole.KubeGroup, V1ClusterRole.KubeKind, "operator-role"),
                    Subjects = new List<V1Subject>
                    {
                        new(V1ServiceAccount.KubeKind, "default", namespaceProperty: "system"),
                    },
                },
                Format));
        fileWriter.Add(
            $"kustomization.{Format.ToString().ToLower()}",
            EntitySerializer.Serialize(
                new KustomizationConfig
                {
                    Resources = new List<string>
                    {
                        $"operator-role.{Format.ToString().ToLower()}",
                        $"operator-role-binding.{Format.ToString().ToLower()}",
                    },
                    CommonLabels = new Dictionary<string, string> { { "operator-element", "rbac" }, },
                },
                Format));

        await fileWriter.OutputAsync(OutputPath);
        return ExitCodes.Success;
    }
}
