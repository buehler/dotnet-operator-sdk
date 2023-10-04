using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text.Json;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher;

internal class ResourceWatcher<TEntity> : IHostedService
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly ILogger<ResourceWatcher<TEntity>> _logger;
    private readonly IServiceProvider _provider;
    private readonly IKubernetesClient<TEntity> _client;
    private readonly TimedEntityQueue<TEntity> _queue;
    private readonly ConcurrentDictionary<string, long> _entityCache = new();
    private readonly Lazy<List<FinalizerRegistration>> _finalizers;

    private Watcher<TEntity>? _watcher;

    public ResourceWatcher(
        ILogger<ResourceWatcher<TEntity>> logger,
        IServiceProvider provider,
        IKubernetesClient<TEntity> client,
        TimedEntityQueue<TEntity> queue)
    {
        _logger = logger;
        _provider = provider;
        _client = client;
        _queue = queue;
        _finalizers = new(() => _provider.GetServices<FinalizerRegistration>().ToList());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting resource watcher for {ResourceType}.", typeof(TEntity).Name);
        _queue.RequeueRequested += OnEntityRequeue;
        WatchResource();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping resource watcher for {ResourceType}.", typeof(TEntity).Name);
        StopWatching();
        _queue.RequeueRequested -= OnEntityRequeue;
        _queue.Clear();
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

        _watcher = _client.Watch(OnEvent, OnError, OnClosed);
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

    private async void OnEntityRequeue(object? sender, (string Name, string? Namespace) queued)
    {
        _logger.LogTrace(
            """Execute requested requeued reconciliation for "{name}".""",
            queued.Name);

        if (await _client.GetAsync(queued.Name, queued.Namespace) is not { } entity)
        {
            _logger.LogWarning(
                """Requeued entity "{name}" was not found. Skip reconciliation.""",
                queued.Name);
            return;
        }

        _entityCache.TryRemove(entity.Uid(), out _);
        await ReconcileModification(entity);
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

        _queue.RemoveIfQueued(entity);

        try
        {
            switch (type)
            {
                case WatchEventType.Added or WatchEventType.Modified:
                    switch (entity)
                    {
                        case { Metadata.DeletionTimestamp: null }:
                            await ReconcileModification(entity);
                            break;
                        case { Metadata: { DeletionTimestamp: not null, Finalizers.Count: > 0 } }:
                            await ReconcileFinalizer(entity);
                            break;
                    }

                    break;
                case WatchEventType.Deleted:
                    await ReconcileDeletion(entity);
                    break;
                default:
                    _logger.LogWarning(
                        """Received unsupported event "{eventType}" for "{kind}/{name}".""",
                        type,
                        entity.Kind,
                        entity.Name());
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Reconciliation of {eventType} for {kind}/{name} failed.",
                type,
                entity.Kind,
                entity.Name());
        }
    }

    private async Task ReconcileModification(TEntity entity)
    {
        var latestGeneration = _entityCache.GetOrAdd(entity.Uid(), 0);
        if (entity.Generation() <= latestGeneration)
        {
            _logger.LogDebug(
                """Entity "{kind}/{name}" modification did not modify generation. Skip event.""",
                entity.Kind,
                entity.Name());
            return;
        }

        _entityCache.TryUpdate(entity.Uid(), entity.Generation() ?? 1, latestGeneration);
        await using var scope = _provider.CreateAsyncScope();
        var controller = scope.ServiceProvider.GetRequiredService<IEntityController<TEntity>>();
        await controller.ReconcileAsync(entity);
    }

    private async Task ReconcileDeletion(TEntity entity)
    {
        await using var scope = _provider.CreateAsyncScope();
        var controller = scope.ServiceProvider.GetRequiredService<IEntityController<TEntity>>();
        await controller.DeletedAsync(entity);
    }

    private async Task ReconcileFinalizer(TEntity entity)
    {
        var pendingFinalizer = entity.Finalizers();
        if (_finalizers.Value.Find(reg =>
                reg.EntityType == entity.GetType() && pendingFinalizer.Contains(reg.Identifier)) is not
            { Identifier: var identifier, FinalizerType: var type })
        {
            _logger.LogDebug(
                """Entity "{kind}/{name}" is finalizing but this operator has no registered finalizers for it.""",
                entity.Kind,
                entity.Name());
            return;
        }

        if (_provider.GetRequiredService(type) is not IEntityFinalizer<TEntity> finalizer)
        {
            _logger.LogError(
                """Finalizer "{identifier}" was no IEntityFinalizer<TEntity>.""",
                identifier);
            return;
        }

        await finalizer.FinalizeAsync(entity);
        entity.RemoveFinalizer(identifier);
        await _client.UpdateAsync(entity);
        _logger.LogInformation(
            """Entity "{kind}/{name}" finalized with "{finalizer}".""",
            entity.Kind,
            entity.Name(),
            identifier);
    }
}
