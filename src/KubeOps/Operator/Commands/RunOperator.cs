using System.Threading.Tasks;
using DotnetKubernetesClient;
using KubeOps.Operator.Commands.Generators;
using KubeOps.Operator.Commands.Management;
using KubeOps.Operator.Commands.Utilities;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Commands
{
    [Command(Description = "Runs the operator.")]
    [Subcommand(typeof(Generator))]
    [Subcommand(typeof(Install))]
    [Subcommand(typeof(Uninstall))]
    [Subcommand(typeof(Management.Webhooks.Webhooks))]
    [Subcommand(typeof(Version))]
    internal class RunOperator
    {
        private readonly IHost _host;
        private readonly IKubernetesClient _client;
        private OperatorSettings _settings;

        public RunOperator(IHost host, IKubernetesClient client, OperatorSettings settings)
        {
            _host = host;
            _client = client;
            _settings = settings;
        }

        [Option(
            CommandOptionType.SingleOrNoValue,
            Description =
                "The namespace - if any - that the operator should limit watching to. If empty, the current namespace is deduced.")]
        public (bool HasValue, string Value) Namespaced { get; set; }

        public async Task OnExecuteAsync()
        {
            if (Namespaced.HasValue && !string.IsNullOrWhiteSpace(Namespaced.Value))
            {
                // The namespace is predefined.
                _settings.Namespace = Namespaced.Value;
            }
            else if (Namespaced.HasValue)
            {
                // Namespacing is requested and the namespace should be deduced by IKubernetesClient.
                _settings.Namespace = await _client.GetCurrentNamespace();
            }

            await _host.RunAsync();
        }
    }
}
