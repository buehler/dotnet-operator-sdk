using KubeOps.Abstractions.Certificates;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Web.Webhooks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Web.Certificates;

internal class CertificateWebhookService(ILogger<CertificateWebhookService> logger, IKubernetesClient client, WebhookLoader loader, WebhookConfig config, ICertificateProvider provider)
    : WebhookServiceBase(client, loader, config), IHostedService
{
    private readonly ILogger<CertificateWebhookService> _logger = logger;
    private readonly ICertificateProvider _provider = provider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CaBundle = _provider.Server.Certificate.EncodeToPemBytes();

        _logger.LogDebug("Registering webhooks");
        await RegisterAll();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }
}
