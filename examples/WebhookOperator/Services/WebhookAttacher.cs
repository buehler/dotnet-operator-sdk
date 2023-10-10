using k8s;
using k8s.Models;

using Localtunnel;
using Localtunnel.Connections;
using Localtunnel.Tunnels;

using WebhookOperator.Entities;

namespace WebhookOperator.Services;

public class WebhookAttacher : IHostedService
{
    private readonly LocaltunnelClient _localtunnelClient = new();
    private Tunnel? _tunnel;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _tunnel ??= await _localtunnelClient.OpenAsync(
            handle => new ProxiedHttpTunnelConnection(
                handle,
                new() { Host = "localhost", Port = 5000, RequestProcessor = null, }),
            cancellationToken: cancellationToken);

        using var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());

        var config = new V1ValidatingWebhookConfiguration().Initialize();
        config.Metadata.Name = "webhook-operator-dev";
        var webhook = new V1ValidatingWebhook();
        webhook.Name = _tunnel.Information.Url.Host;
        webhook.AdmissionReviewVersions = new List<string> { "v1" };
        webhook.SideEffects = "None";
        webhook.MatchPolicy = "Exact";
        webhook.Rules = new List<V1RuleWithOperations>
        {
            new()
            {
                Operations = new[] { "*" },
                Resources = new[] { "testentitys" },
                Scope = "*",
                ApiGroups = new[] { "webhook.dev" },
                ApiVersions = new[] { "*" },
            },
        };
        webhook.ClientConfig =
            new Admissionregistrationv1WebhookClientConfig
            {
                Url = $"{_tunnel.Information.Url}validate/{nameof(V1TestEntity).ToLowerInvariant()}",
            };

        config.Webhooks = new List<V1ValidatingWebhook> { webhook };

        switch (await client.AdmissionregistrationV1.ListValidatingWebhookConfigurationAsync(
                    fieldSelector: $"metadata.name={config.Name()}", cancellationToken: cancellationToken))
        {
            case { Items: [var existing] }:
                config.Metadata.ResourceVersion = existing.ResourceVersion();
                await client.AdmissionregistrationV1.ReplaceValidatingWebhookConfigurationAsync(config, config.Name(),
                    cancellationToken: cancellationToken);
                break;
            default:
                await client.AdmissionregistrationV1.CreateValidatingWebhookConfigurationAsync(config,
                    cancellationToken: cancellationToken);
                break;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _tunnel?.Stop();
        return Task.CompletedTask;
    }
}
