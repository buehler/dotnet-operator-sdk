using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher;

internal class ResourceWatcher<TEntity> : IHostedService
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly ILogger<ResourceWatcher<TEntity>> _logger;
    private readonly GenericClient _client;

    private Watcher<TEntity>? _watcher;

    public ResourceWatcher(ILogger<ResourceWatcher<TEntity>> logger, EntityMetadata<TEntity> metadata)
    {
        _logger = logger;

        var kubernetes = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        _client = metadata.Group switch
        {
            null => new GenericClient(
                kubernetes,
                metadata.Version,
                metadata.PluralName),
            _ => new GenericClient(
                kubernetes,
                metadata.Group,
                metadata.Version,
                metadata.PluralName),
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting resource watcher for {ResourceType}.", typeof(TEntity).Name);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping resource watcher for {ResourceType}.", typeof(TEntity).Name);
    }

    private async Task WatchResource()
    {
        if (_watcher != null)
        {
            if (!_watcher.Watching)
            {
                _watcher.Dispose();
            }
            else
            {
                _logger.LogTrace("""Watcher for type "{type}" already running.""", typeof(TEntity));
                return;
            }
        }
        
        _client.
    }
}
