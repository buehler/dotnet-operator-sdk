using System.Collections.Generic;
using k8s.Models;

namespace KubeOps.Operator.Webhooks.ConversionWebhook;

internal interface IConversionWebhookBuilder
{
    IEnumerable<V1CustomResourceDefinition> BuildWebhookConfiguration(WebhookConfig webhookConfig, bool isLocalTunnel);
}
