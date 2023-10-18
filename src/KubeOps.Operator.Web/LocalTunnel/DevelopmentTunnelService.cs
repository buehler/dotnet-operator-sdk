using k8s;
using k8s.Models;

using KubeOps.KubernetesClient;
using KubeOps.Transpiler;

using Localtunnel;
using Localtunnel.Endpoints.Http;
using Localtunnel.Handlers.Kestrel;
using Localtunnel.Processors;
using Localtunnel.Tunnels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Web.LocalTunnel;

internal class DevelopmentTunnelService : IHostedService
{
    private readonly TunnelConfig _config;
    private readonly WebhookLoader _loader;
    private readonly LocaltunnelClient _tunnelClient;
    private Tunnel? _tunnel;

    public DevelopmentTunnelService(ILoggerFactory loggerFactory, TunnelConfig config, WebhookLoader loader)
    {
        _config = config;
        _loader = loader;
        _tunnelClient = new(loggerFactory);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _tunnel = await _tunnelClient.OpenAsync(
            new KestrelTunnelConnectionHandler(
                new HttpRequestProcessingPipelineBuilder()
                    .Append(new HttpHostHeaderRewritingRequestProcessor(_config.Hostname)).Build(),
                new HttpTunnelEndpointFactory(_config.Hostname, _config.Port)),
            cancellationToken: cancellationToken);
        await _tunnel.StartAsync(cancellationToken: cancellationToken);
        await RegisterValidators(_tunnel.Information.Url);
        await RegisterMutators(_tunnel.Information.Url);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _tunnel?.Dispose();
        return Task.CompletedTask;
    }

    private async Task RegisterValidators(Uri uri)
    {
        var validationWebhooks = _loader
            .ValidationWebhooks
            .Select(t => (HookTypeName: t.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant(),
                Entities.ToEntityMetadata(t.BaseType!.GenericTypeArguments[0]).Metadata))
            .Select(hook => new V1ValidatingWebhook
            {
                Name = $"validate.{hook.Metadata.SingularName}.{hook.Metadata.Group}.{hook.Metadata.Version}",
                MatchPolicy = "Exact",
                AdmissionReviewVersions = new[] { "v1" },
                SideEffects = "None",
                Rules = new[]
                {
                    new V1RuleWithOperations
                    {
                        Operations = new[] { "*" },
                        Resources = new[] { hook.Metadata.PluralName },
                        ApiGroups = new[] { hook.Metadata.Group },
                        ApiVersions = new[] { hook.Metadata.Version },
                    },
                },
                ClientConfig = new Admissionregistrationv1WebhookClientConfig
                {
                    Url = $"{uri}validate/{hook.HookTypeName}",
                },
            });

        var validatorConfig = new V1ValidatingWebhookConfiguration(
            metadata: new V1ObjectMeta(name: "dev-validators"),
            webhooks: validationWebhooks.ToList()).Initialize();

        if (validatorConfig.Webhooks.Any())
        {
            using var validatorClient = new KubernetesClient.KubernetesClient() as IKubernetesClient;
            await validatorClient.SaveAsync(validatorConfig);
        }
    }

    private async Task RegisterMutators(Uri uri)
    {
        var mutationWebhooks = _loader
            .MutationWebhooks
            .Select(t => (HookTypeName: t.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant(),
                Entities.ToEntityMetadata(t.BaseType!.GenericTypeArguments[0]).Metadata))
            .Select(hook => new V1MutatingWebhook
            {
                Name = $"mutate.{hook.Metadata.SingularName}.{hook.Metadata.Group}.{hook.Metadata.Version}",
                MatchPolicy = "Exact",
                AdmissionReviewVersions = new[] { "v1" },
                SideEffects = "None",
                Rules = new[]
                {
                    new V1RuleWithOperations
                    {
                        Operations = new[] { "*" },
                        Resources = new[] { hook.Metadata.PluralName },
                        ApiGroups = new[] { hook.Metadata.Group },
                        ApiVersions = new[] { hook.Metadata.Version },
                    },
                },
                ClientConfig = new Admissionregistrationv1WebhookClientConfig
                {
                    Url = $"{uri}mutate/{hook.HookTypeName}",
                },
            });

        var mutatorConfig = new V1MutatingWebhookConfiguration(
            metadata: new V1ObjectMeta(name: "dev-mutators"),
            webhooks: mutationWebhooks.ToList()).Initialize();

        if (mutatorConfig.Webhooks.Any())
        {
            using var mutatorClient = new KubernetesClient.KubernetesClient() as IKubernetesClient;
            await mutatorClient.SaveAsync(mutatorConfig);
        }
    }
}
