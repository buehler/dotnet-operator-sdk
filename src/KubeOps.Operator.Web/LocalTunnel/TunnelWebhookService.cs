using KubeOps.KubernetesClient;
using KubeOps.Operator.Web.Certificates;
using KubeOps.Operator.Web.Webhooks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Web.LocalTunnel
{
    internal class TunnelWebhookService(ILogger<CertificateWebhookService> logger, IKubernetesClient client, WebhookLoader loader, WebhookConfig config, DevelopmentTunnel developmentTunnel)
        : WebhookServiceBase(client, loader, config), IHostedService
    {
        private readonly ILogger<CertificateWebhookService> _logger = logger;
        private readonly DevelopmentTunnel _developmentTunnel = developmentTunnel;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Uri = await _developmentTunnel.StartAsync(cancellationToken);

            _logger.LogDebug("Registering webhooks");
            await RegisterAll();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _developmentTunnel.Dispose();
            return Task.CompletedTask;
        }
    }
}
