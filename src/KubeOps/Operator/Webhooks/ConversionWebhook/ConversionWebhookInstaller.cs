using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace KubeOps.Operator.Webhooks.ConversionWebhook;

internal class ConversionWebhookInstaller : IConversionWebhookInstaller
{
    private readonly IConversionWebhookBuilder _conversionWebhookBuilder;
    private readonly IKubernetesClient _kubernetesClient;
    private readonly ILogger<ConversionWebhookInstaller> _logger;

    public ConversionWebhookInstaller(IConversionWebhookBuilder conversionWebhookBuilder, IKubernetesClient kubernetesClient, ILogger<ConversionWebhookInstaller> logger)
    {
        _conversionWebhookBuilder = conversionWebhookBuilder;
        _kubernetesClient = kubernetesClient;
        _logger = logger;
    }

    public async Task InstallConversionWebhooks(WebhookConfig webhookConfig, bool isLocalTunnel = false)
    {
        var crdsToUpdateWithConversionWebhook = _conversionWebhookBuilder.BuildWebhookConfiguration(webhookConfig, isLocalTunnel);
        foreach (V1CustomResourceDefinition crd in crdsToUpdateWithConversionWebhook)
        {
            try
            {
                await _kubernetesClient.Save(crd);
            }
            catch (HttpOperationException ex)
            {
                _logger.LogCritical("Error saving crd: {Response}", ex.Response.Content);
                throw;
            }

            _logger.LogInformation("Registered conversion webhook for {Crd}", crd.Spec.Names.Kind);
        }
    }
}
