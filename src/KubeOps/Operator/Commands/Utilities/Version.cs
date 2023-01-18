using KubeOps.KubernetesClient;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Utilities;

[Command(
    "version",
    "v",
    Description = "Prints the actual server version of the connected kubernetes cluster.")]
internal class Version
{
    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var client = app.GetRequiredService<IKubernetesClient>();
        var version = await client.GetServerVersion();
        await app.Out.WriteLineAsync(
            $@"The kubernetes api reported the following version:
Git-Version: {version.GitVersion}
Major:       {version.Major}
Minor:       {version.Minor}
Platform:    {version.Platform}");

        return ExitCodes.Success;
    }
}
