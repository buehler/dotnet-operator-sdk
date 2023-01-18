using KubeOps.KubernetesClient;
using Localtunnel;
using Localtunnel.Connections;
using Localtunnel.Tunnels;

namespace KubeOps.Operator.Webhooks;

internal class WebhookLocalTunnel : IHostedService, IDisposable
{
    private readonly ILogger<WebhookLocalTunnel> _logger;
    private readonly OperatorSettings _settings;
    private readonly IKubernetesClient _kubernetesClient;
    private readonly MutatingWebhookConfigurationBuilder _mutatorBuilder;
    private readonly ValidatingWebhookConfigurationBuilder _validatorBuilder;
    private readonly LocaltunnelClient _localtunnelClient = new();
    private Tunnel? _tunnel;

    public WebhookLocalTunnel(
        ILogger<WebhookLocalTunnel> logger,
        OperatorSettings settings,
        IKubernetesClient kubernetesClient,
        MutatingWebhookConfigurationBuilder mutatorBuilder,
        ValidatingWebhookConfigurationBuilder validatorBuilder)
    {
        _logger = logger;
        _settings = settings;
        _kubernetesClient = kubernetesClient;
        _mutatorBuilder = mutatorBuilder;
        _validatorBuilder = validatorBuilder;
    }

    public string Host { get; init; } = string.Empty;

    public short Port { get; init; }

    public bool IsHttps { get; init; }

    public bool AllowUntrustedCertificates { get; init; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Try to open localtunnel.");
        try
        {
            _tunnel ??= await _localtunnelClient.OpenAsync(
                handle => IsHttps
                    ? new ProxiedSslTunnelConnection(
                        handle,
                        new()
                        {
                            Host = Host,
                            Port = Port,
                            AllowUntrustedCertificates = AllowUntrustedCertificates,
                            RequestProcessor = null,
                        })
                    : new ProxiedHttpTunnelConnection(
                        handle,
                        new() { Host = Host, Port = Port, RequestProcessor = null, }),
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The localtunnel could not be started! Proceeding without.");
            _tunnel = null;
            return;
        }

        _logger.LogDebug(@"Created localtunnel with id ""{id}""", _tunnel.Information.Id);
        await _tunnel.StartAsync();

        _logger.LogDebug(
            @"Started localtunnel with id ""{id}"" on ""{url}""",
            _tunnel.Information.Id,
            _tunnel.Information.Url);

        // Register the webhook in Kubernetes.
        var webhookConfig = new WebhookConfig(
            _settings.Name,
            _tunnel.Information.Url.ToString(),
            null,
            null);

        var validatorConfig = _validatorBuilder.BuildWebhookConfiguration(webhookConfig);
        var mutatorConfig = _mutatorBuilder.BuildWebhookConfiguration(webhookConfig);

        await _kubernetesClient.Save(validatorConfig);
        await _kubernetesClient.Save(mutatorConfig);

        _logger.LogInformation(
            @"Started localtunnel with id ""{id}"" on ""{url}"" and created kubernetes webhook configs for ""{name}""",
            _tunnel.Information.Id,
            _tunnel.Information.Url,
            _settings.Name);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tunnel == null)
        {
            _logger.LogInformation("No tunnel available to stop.");
            return Task.CompletedTask;
        }

        _tunnel.Stop();
        _logger.LogInformation(
            @"Stopped localtunnel with id ""{id}"" on ""{url}""",
            _tunnel.Information.Id,
            _tunnel.Information.Url);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _tunnel?.Dispose();
        _localtunnelClient.Dispose();
    }
}
