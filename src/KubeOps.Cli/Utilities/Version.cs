using k8s;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Cli.Utilities;

[Command(
    "version",
    "v",
    Description = "Prints the actual server version of the connected kubernetes cluster. (Aliases: v)")]
internal class Version
{
    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var client = app.GetRequiredService<IKubernetes>();
        var version = await client.Version.GetCodeAsync();
        await app.Out.WriteLineAsync(
            $"""
             The Kubernetes API reported the following version:
             Git-Version: {version.GitVersion}
             Major:       {version.Major}
             Minor:       {version.Minor}
             Platform:    {version.Platform}
             """);

        return ExitCodes.Success;
    }
}
