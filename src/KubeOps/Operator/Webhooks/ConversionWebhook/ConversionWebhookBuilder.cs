using System.Collections.Generic;
using System.Linq;
using DotnetKubernetesClient.Entities;
using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Extensions;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Webhooks.ConversionWebhook;

internal class ConversionWebhookBuilder : IConversionWebhookBuilder
{
    private readonly ICrdBuilder _crdBuilder;
    private readonly IComponentRegistrar _componentRegistrar;
    private readonly ILogger<ConversionWebhookBuilder> _logger;

    public ConversionWebhookBuilder(ICrdBuilder crdBuilder, IComponentRegistrar componentRegistrar, ILogger<ConversionWebhookBuilder> logger)
    {
        _crdBuilder = crdBuilder;
        _componentRegistrar = componentRegistrar;
        _logger = logger;
    }

    public IEnumerable<V1CustomResourceDefinition> BuildWebhookConfiguration(WebhookConfig webhookConfig, bool isLocalTunnel)
    {
        _logger.LogInformation("Building conversionwebhook configuration");
        var allWebhooks = _componentRegistrar.ConversionRegistrations;
        if (allWebhooks.Count == 0)
        {
            return new List<V1CustomResourceDefinition>();
        }

        var customResourceDefinitionsToUpdate = _crdBuilder.BuildCrds()
            .Where(
                crd => crd.Spec.Versions.Count > 1 &&
                       allWebhooks.Any(
                           cr =>
                           {
                               return crd.Spec.Versions.Any(
                                          v => cr.EntityIn.CreateResourceDefinition().GroupVersion() ==
                                               $"{crd.Spec.Group}/{v.Name}") ||
                                      crd.Spec.Versions.Any(
                                          v => cr.EntityOut.CreateResourceDefinition().GroupVersion() ==
                                               $"{crd.Spec.Group}/{v.Name}");
                           })).ToList();
        _logger.LogInformation("Found {AmountOfCustomResourceDefinitions} to update", customResourceDefinitionsToUpdate.Count);
        foreach (var crd in customResourceDefinitionsToUpdate)
        {
            crd.Spec.Conversion = GetConversionWebhookConfiguration(webhookConfig, isLocalTunnel);
        }

        return customResourceDefinitionsToUpdate;
    }

    private static V1CustomResourceConversion GetConversionWebhookConfiguration(WebhookConfig webhookConfig, bool isLocalTunnel = false)
    {
        if (isLocalTunnel)
        {
            return new V1CustomResourceConversion(
                "Webhook",
                new V1WebhookConversion(
                    new List<string> { "v1" },
                    new Apiextensionsv1WebhookClientConfig
                    {
                        Url = $"{webhookConfig.BaseUrl}convert",
                    }));
        }

        return new V1CustomResourceConversion(
            "Webhook",
            new V1WebhookConversion(
                new List<string> { "v1" },
                new Apiextensionsv1WebhookClientConfig
                {
                    CaBundle = webhookConfig.CaBundle,
                    Service = new Apiextensionsv1ServiceReference(webhookConfig.Service?.Name, webhookConfig.Service?.NamespaceProperty, webhookConfig.Service?.Path, webhookConfig.Service?.Port),
                }));
    }
}
