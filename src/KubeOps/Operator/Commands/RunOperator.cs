using KubeOps.KubernetesClient;
using KubeOps.Operator.Commands.Generators;
using KubeOps.Operator.Commands.Management;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands;

[Command(Description = "Runs the operator.")]
[Subcommand(typeof(Generator))]
[Subcommand(typeof(Install))]
[Subcommand(typeof(Uninstall))]
[Subcommand(typeof(Management.Webhooks.Webhooks))]
[Subcommand(typeof(Version))]
internal class RunOperator
{
    private readonly IHost _host;
    private readonly OperatorSettings _settings;

    public RunOperator(IHost host, OperatorSettings settings)
    {
        _host = host;
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
            var client = _host.Services.GetRequiredService<IKubernetesClient>();

            // Namespacing is requested and the namespace should be deduced by IKubernetesClient.
            _settings.Namespace = await client.GetCurrentNamespace();
        }

        await _host.RunAsync();
    }
}
