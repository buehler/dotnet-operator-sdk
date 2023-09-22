using System.Runtime.Serialization;
using System.Text.Json;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Operator.Client;

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
        IKubernetesClientFactory factory)
    {
        _logger = logger;
        _provider = provider;
        _client = factory.GetClient<TEntity>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting resource watcher for {ResourceType}.", typeof(TEntity).Name);
        WatchResource();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping resource watcher for {ResourceType}.", typeof(TEntity).Name);
        StopWatching();
        return Task.CompletedTask;
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

    private void StopWatching()
    {
        _watcher?.Dispose();
    }

    private void OnClosed()
    {
        _logger.LogDebug("The server closed the connection. Trying to reconnect.");
        WatchResource();
    }

    private void OnError(Exception e)
    {
        switch (e)
        {
            case SerializationException when
                e.InnerException is JsonException &&
                e.InnerException.Message.Contains("The input does not contain any JSON tokens"):
                _logger.LogDebug(
                    """The watcher received an empty response for resource "{resource}".""",
                    typeof(TEntity));
                return;

            case HttpRequestException when
                e.InnerException is EndOfStreamException &&
                e.InnerException.Message.Contains("Attempted to read past the end of the stream."):
                _logger.LogDebug(
                    """The watcher received a known error from the watched resource "{resource}". This indicates that there are no instances of this resource.""",
                    typeof(TEntity));
                return;
        }

        _logger.LogError(e, """There was an error while watching the resource "{resource}".""", typeof(TEntity));
        WatchResource();
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
        try
        {
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
        catch (Exception e)
        {
            _logger.LogWarning(
                e,
                "Reconciliation of {eventType} for {kind}/{name} failed.",
                type,
                entity.Kind,
                entity.Name());
        }
    }
}
