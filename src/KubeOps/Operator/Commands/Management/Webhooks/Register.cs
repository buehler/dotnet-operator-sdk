using System.Text;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Webhooks;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Management.Webhooks;

[Command(
    "register",
    "reg",
    "r",
    Description = "Registers the currently implemented webhooks in the current selected cluster.")]
internal class Register
{
    private readonly OperatorSettings _settings;
    private readonly IComponentRegistrar _componentRegistrar;
    private readonly ValidatingWebhookConfigurationBuilder _validatingWebhookConfigurationBuilder;
    private readonly MutatingWebhookConfigurationBuilder _mutatingWebhookConfigurationBuilder;

    public Register(
        OperatorSettings settings,
        IComponentRegistrar componentRegistrar,
        MutatingWebhookConfigurationBuilder mutatingWebhookConfigurationBuilder,
        ValidatingWebhookConfigurationBuilder validatingWebhookConfigurationBuilder)
    {
        _settings = settings;
        _componentRegistrar = componentRegistrar;
        _mutatingWebhookConfigurationBuilder = mutatingWebhookConfigurationBuilder;
        _validatingWebhookConfigurationBuilder = validatingWebhookConfigurationBuilder;
    }

    [Option(
        Description =
            "The base-url under which the webhooks are registered (e.g. https://foobar.ngrok.com/). " +
            "Either base url or service info must be set.")]
    public string? BaseUrl { get; init; }

    [Option(
        Description =
            "The name of a kubernetes service that should be used for communication. " +
            "Either base url or service info must be set.",
        Template = "--service-name <NAME>")]
    public string? ServiceName { get; init; }

    [Option(
        Description =
            "The namespace of the kubernetes service that should be used for communication. " +
            "Either base url or service info must be set.",
        Template = "--service-namespace <NAMESPACE>")]
    public string? ServiceNamespace { get; init; }

    [Option(
        Description =
            "The - if any - path to the specific route on the service. " +
            "Either base url or service info must be set.",
        Template = "--service-path <PATH>")]
    public string? ServicePath { get; init; }

    [Option(
        Description =
            "The port of the service that should be used (defaults to 443). " +
            "Either base url or service info must be set.",
        Template = "--service-port <PORT>")]
    public short? ServicePort { get; init; }

    [Option(
        Description =
            "The (pem encoded) ca-certificate - if any - bundle to validate the webhook server. " +
            "Either base url or service info must be set.",
        Template = "--ca-bundle <CA_PEM_DATA>")]
    public string? CaBundle { get; init; }

    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var client = app.GetRequiredService<IKubernetesClient>();
        await app.Out.WriteLineAsync(
            $"Found {_componentRegistrar.ValidatorRegistrations.Count} validator registrations.");
        await app.Out.WriteLineAsync(
            $"Found {_componentRegistrar.MutatorRegistrations.Count} mutator registrations.");

        var webhookConfig = new WebhookConfig(
            _settings.Name,
            BaseUrl,
            CaBundle != null ? Encoding.UTF8.GetBytes(CaBundle) : null,
            ServiceName != null && ServiceNamespace != null
                ? new Admissionregistrationv1ServiceReference(
                    ServiceName,
                    ServiceNamespace,
                    ServicePath,
                    ServicePort)
                : null);

        var validatorConfig = _validatingWebhookConfigurationBuilder.BuildWebhookConfiguration(webhookConfig);
        var mutatorConfig = _mutatingWebhookConfigurationBuilder.BuildWebhookConfiguration(webhookConfig);

        await app.Out.WriteLineAsync($@"Install ""{validatorConfig.Metadata.Name}"" validator on cluster.");
        await app.Out.WriteLineAsync($@"Install ""{mutatorConfig.Metadata.Name}"" mutator on cluster.");
        await client.Save(validatorConfig);
        await client.Save(mutatorConfig);

        return ExitCodes.Success;
    }
}
