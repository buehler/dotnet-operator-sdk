using System.Threading.Tasks;
using KubeOps.Operator.Client;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Utilities
{
    [Command(
        "version",
        "v",
        Description = "Prints the actual server version of the connected kubernetes cluster.")]
    internal class Version
    {
        private readonly IKubernetesClient _client;

        public Version(IKubernetesClient client)
        {
            _client = client;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var version = await _client.GetServerVersion();
            await app.Out.WriteLineAsync(
                $@"The kubernetes api reported the following version:
Git-Version: {version.GitVersion}
Major:       {version.Major}
Minor:       {version.Minor}
Platform:    {version.Platform}");

            return ExitCodes.Success;
        }
    }
}
