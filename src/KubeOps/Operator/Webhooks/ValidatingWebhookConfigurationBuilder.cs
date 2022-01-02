using k8s.Models;
using KubeOps.Operator.Util;

namespace KubeOps.Operator.Webhooks;

internal class ValidatingWebhookConfigurationBuilder
{
    private readonly ValidatingWebhookBuilder _webhookBuilder;

    public ValidatingWebhookConfigurationBuilder(ValidatingWebhookBuilder webhookBuilder)
    {
        _webhookBuilder = webhookBuilder;
    }

    public V1ValidatingWebhookConfiguration BuildWebhookConfiguration(WebhookConfig webhookConfig) =>
        new()
        {
            Kind = V1ValidatingWebhookConfiguration.KubeKind,
            ApiVersion =
                $"{V1ValidatingWebhookConfiguration.KubeGroup}/{V1ValidatingWebhookConfiguration.KubeApiVersion}",
            Metadata = new() { Name = webhookConfig.OperatorName.TrimWebhookName("validators."), },
            Webhooks = _webhookBuilder.BuildWebhooks(webhookConfig),
        };
}
