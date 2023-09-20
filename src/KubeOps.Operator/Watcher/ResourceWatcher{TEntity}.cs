using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher;

internal class ResourceWatcher<TEntity> : IHostedService
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly ILogger<ResourceWatcher<TEntity>> _logger;
    private readonly IServiceProvider _provider;
    private readonly GenericClient _client;

    private Watcher<TEntity>? _watcher;

    public ResourceWatcher(
        ILogger<ResourceWatcher<TEntity>> logger,
        IServiceProvider provider,
        EntityMetadata<TEntity> metadata)
    {
        _logger = logger;
        _provider = provider;

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
        WatchResource();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping resource watcher for {ResourceType}.", typeof(TEntity).Name);
    }

    private void WatchResource()
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

        _watcher = _client.Watch<TEntity>(OnEvent, OnError, OnClosed);
    }

    private void OnClosed()
    {
        throw new NotImplementedException();
    }

    private void OnError(Exception obj)
    {
        throw new NotImplementedException();
    }

    private async void OnEvent(WatchEventType type, TEntity entity)
    {
        _logger.LogTrace(
            """Received watch event "{eventType}" for "{kind}/{name}".""",
            type,
            entity.Kind,
            entity.Name());

        if (type is WatchEventType.Bookmark or WatchEventType.Error)
        {
            return;
        }

        await using var scope = _provider.CreateAsyncScope();
        var controller = scope.ServiceProvider.GetRequiredService<IEntityController<TEntity>>();
        switch (type)
        {
            case WatchEventType.Added:
            case WatchEventType.Modified:
                await controller.ReconcileAsync(entity);
                break;
            case WatchEventType.Deleted:
                await controller.DeletedAsync(entity);
                break;
            default:
                _logger.LogWarning(
                    """Received unknown watch event type "{eventType}" for "{kind}/{name}".""",
                    type,
                    entity.Kind,
                    entity.Name());
                break;
        }
    }
}
