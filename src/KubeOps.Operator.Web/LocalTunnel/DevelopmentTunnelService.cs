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

internal class DevelopmentTunnelService(ILoggerFactory loggerFactory, IKubernetesClient client, TunnelConfig config, WebhookLoader loader)
    : IHostedService
{
    private readonly LocaltunnelClient _tunnelClient = new(loggerFactory);
    private Tunnel? _tunnel;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _tunnel = await _tunnelClient.OpenAsync(
            new KestrelTunnelConnectionHandler(
                new HttpRequestProcessingPipelineBuilder()
                    .Append(new HttpHostHeaderRewritingRequestProcessor(config.Hostname)).Build(),
                new HttpTunnelEndpointFactory(config.Hostname, config.Port)),
            cancellationToken: cancellationToken);
        await _tunnel.StartAsync(cancellationToken: cancellationToken);
        await RegisterValidators(_tunnel.Information.Url);
        await RegisterMutators(_tunnel.Information.Url);
        await RegisterConverters(_tunnel.Information.Url);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _tunnel?.Dispose();
        return Task.CompletedTask;
    }

    private async Task RegisterValidators(Uri uri)
    {
        var hookName = string.Join(".", 
            new List<string> {
                hook.Metadata.SingularName, hook.Metadata.Group, hook.Metadata.Version
            }.Where(name => !string.IsNullOrWhiteSpace(name))
        );
        var validationWebhooks = loader
            .ValidationWebhooks
            .Select(t => (HookTypeName: t.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant(),
                Entities.ToEntityMetadata(t.BaseType!.GenericTypeArguments[0]).Metadata))
            .Select(hook => new V1ValidatingWebhook
            {
                Name = $"validate.{hookName}",
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
            await client.SaveAsync(validatorConfig);
        }
    }

    private async Task RegisterMutators(Uri uri)
    {
        var hookName = string.Join(".", 
            new List<string> {
                hook.Metadata.SingularName, hook.Metadata.Group, hook.Metadata.Version
            }.Where(name => !string.IsNullOrWhiteSpace(name))
        );
        var mutationWebhooks = loader
            .MutationWebhooks
            .Select(t => (HookTypeName: t.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant(),
                Entities.ToEntityMetadata(t.BaseType!.GenericTypeArguments[0]).Metadata))
            .Select(hook => new V1MutatingWebhook
            {
                Name = $"mutate.{hookName}",
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
            await client.SaveAsync(mutatorConfig);
        }
    }

    private async Task RegisterConverters(Uri uri)
    {
        var conversionWebhooks = loader.ConversionWebhooks.ToList();
        if (conversionWebhooks.Count == 0)
        {
            return;
        }

        foreach (var wh in conversionWebhooks)
        {
            var metadata = Entities.ToEntityMetadata(wh.BaseType!.GenericTypeArguments[0]).Metadata;
            var crdName = $"{metadata.PluralName}.{metadata.Group}";

            if (await client.GetAsync<V1CustomResourceDefinition>(crdName) is not { } crd)
            {
                continue;
            }

            var whUrl = $"{uri}convert/{metadata.Group}/{metadata.PluralName}";
            crd.Spec.Conversion = new V1CustomResourceConversion("Webhook")
            {
                Webhook = new V1WebhookConversion
                {
                    ConversionReviewVersions = new[] { "v1" },
                    ClientConfig = new Apiextensionsv1WebhookClientConfig { Url = whUrl },
                },
            };

            await client.UpdateAsync(crd);
        }
    }
}
