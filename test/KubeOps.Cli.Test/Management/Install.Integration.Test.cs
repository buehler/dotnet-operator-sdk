using System.CommandLine;
using System.CommandLine.Invocation;

using k8s;

using KubeOps.Cli.Commands.Management;

using Spectre.Console.Testing;

namespace KubeOps.Cli.Test.Management;

public class InstallIntegrationTest
{
    private static readonly string ProjectPath =
        Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "examples", "Operator",
            "Operator.csproj");

    [Fact(Skip =
        "For some reason, the MetadataReferences are not loaded when the assembly parser is used from a test project.")]
    public async Task Should_Install_Crds_In_Cluster()
    {
        var console = new TestConsole();
        var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        var cmd = Install.Command;
        var ctx = new InvocationContext(
            cmd.Parse(ProjectPath, "-f"));

        await Install.Handler(console, client, ctx);
    }
}
