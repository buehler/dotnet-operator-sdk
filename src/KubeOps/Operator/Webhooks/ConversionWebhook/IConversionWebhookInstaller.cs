using System.Threading.Tasks;

namespace KubeOps.Operator.Webhooks.ConversionWebhook;

internal interface IConversionWebhookInstaller
{
    Task InstallConversionWebhooks(WebhookConfig webhookConfig, bool isLocalTunnel = false);
}
