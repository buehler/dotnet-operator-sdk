using k8s.Models;
using KubeOps.Operator.Util;

namespace KubeOps.Operator.Webhooks;

internal class MutatingWebhookConfigurationBuilder
{
    private readonly MutatingWebhookBuilder _webhookBuilder;

    public MutatingWebhookConfigurationBuilder(MutatingWebhookBuilder webhookBuilder)
    {
        _webhookBuilder = webhookBuilder;
    }

    public V1MutatingWebhookConfiguration BuildWebhookConfiguration(WebhookConfig webhookConfig) =>
        new()
        {
            Kind = V1MutatingWebhookConfiguration.KubeKind,
            ApiVersion =
                $"{V1MutatingWebhookConfiguration.KubeGroup}/{V1MutatingWebhookConfiguration.KubeApiVersion}",
            Metadata = new V1ObjectMeta { Name = webhookConfig.OperatorName.TrimWebhookName("mutators."), },
            Webhooks = _webhookBuilder.BuildWebhooks(webhookConfig),
        };
}
