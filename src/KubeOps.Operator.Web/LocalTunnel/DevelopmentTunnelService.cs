using System.Reflection;

using k8s;
using k8s.Models;

using KubeOps.Operator.Client;
using KubeOps.Operator.Web.Webhooks.Mutation;
using KubeOps.Operator.Web.Webhooks.Validation;
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
    private readonly LocaltunnelClient _tunnelClient;
    private Tunnel? _tunnel;

    public DevelopmentTunnelService(ILoggerFactory loggerFactory, TunnelConfig config)
    {
        _config = config;
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

    private static async Task RegisterValidators(Uri uri)
    {
        var validationWebhooks = Assembly
            .GetEntryAssembly()!
            .DefinedTypes
            .Where(t => t.BaseType?.IsGenericType == true &&
                        t.BaseType?.GetGenericTypeDefinition() == typeof(ValidationWebhook<>))
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

        using var validatorClient = KubernetesClientFactory.Create<V1ValidatingWebhookConfiguration>();
        await validatorClient.SaveAsync(validatorConfig);
    }

    private static async Task RegisterMutators(Uri uri)
    {
        var mutationWebhooks = Assembly
            .GetEntryAssembly()!
            .DefinedTypes
            .Where(t => t.BaseType?.IsGenericType == true &&
                        t.BaseType?.GetGenericTypeDefinition() == typeof(MutationWebhook<>))
            .Select(t => (HookTypeName: t.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant(),
                Entities.ToEntityMetadata(t.BaseType!.GenericTypeArguments[0]).Metadata))
            .Select(hook => new V1MutatingWebhook
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

        var mutatorConfig = new V1MutatingWebhookConfiguration(
            metadata: new V1ObjectMeta(name: "dev-mutators"),
            webhooks: mutationWebhooks.ToList()).Initialize();

        using var mutatorClient = KubernetesClientFactory.Create<V1MutatingWebhookConfiguration>();
        await mutatorClient.SaveAsync(mutatorConfig);
    }
}
