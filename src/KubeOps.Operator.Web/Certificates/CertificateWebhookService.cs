using KubeOps.Abstractions.Certificates;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Web.Webhooks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Web.Certificates;

internal class CertificateWebhookService(ILogger<CertificateWebhookService> logger, IKubernetesClient client, WebhookLoader loader, WebhookConfig config, ICertificateProvider provider)
    : WebhookServiceBase(client, loader, config), IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CaBundle = provider.Server.Certificate.EncodeToPemBytes();

        logger.LogDebug("Registering webhooks");
        await RegisterAll();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        provider.Dispose();
        return Task.CompletedTask;
    }
}
