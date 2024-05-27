using System.Collections.Concurrent;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Queue;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher;

internal class ResourceWatcher<TEntity>(
    ILogger<ResourceWatcher<TEntity>> logger,
    IServiceProvider provider,
    TimedEntityQueue<TEntity> requeue,
    OperatorSettings settings,
    IKubernetesClient client)
    : IHostedService, IAsyncDisposable, IDisposable
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly ConcurrentDictionary<string, long> _entityCache = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private Task? _eventWatcher;
    private bool _disposed;

    ~ResourceWatcher()
    {
        Dispose(false);
    }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting resource watcher for {ResourceType}.", typeof(TEntity).Name);

        _eventWatcher = WatchClientEventsAsync(_cancellationTokenSource.Token);

        logger.LogInformation("Started resource watcher for {ResourceType}.", typeof(TEntity).Name);
        return Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping resource watcher for {ResourceType}.", typeof(TEntity).Name);
        if (_disposed)
        {
            return;
        }

#if NET8_0_OR_GREATER
        await _cancellationTokenSource.CancelAsync();
#else
        _cancellationTokenSource.Cancel();
#endif
        if (_eventWatcher is not null)
        {
            await _eventWatcher.WaitAsync(cancellationToken);
        }

        logger.LogInformation("Stopped resource watcher for {ResourceType}.", typeof(TEntity).Name);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _cancellationTokenSource.Dispose();
        _eventWatcher?.Dispose();
        requeue.Dispose();
        client.Dispose();

        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_eventWatcher is not null)
        {
            await CastAndDispose(_eventWatcher);
        }

        await CastAndDispose(_cancellationTokenSource);
        await CastAndDispose(requeue);
        await CastAndDispose(client);

        _disposed = true;

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    private async Task WatchClientEventsAsync(CancellationToken stoppingToken)
    {
        var onError = new BackoffPolicy(stoppingToken, BackoffPolicy.ExponentialWithJitter());
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (resourceVersion, entities) = await client.ListAsync<TEntity>(
                    @namespace: settings.Namespace,
                    cancellationToken: stoppingToken);

                foreach (var entity in entities)
                {
                    await OnEventAsync(WatchEventType.Added, entity, stoppingToken);
                }

                onError.Clear();
                await client.WatchSafeAsync<TEntity>(
                    eventTask: OnEventAsync,
                    @namespace: settings.Namespace,
                    resourceVersion: resourceVersion,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // nothing to do, will end the watch
            }
            catch (Exception cause)
            {
                logger.LogError(cause, "Error while watching resources - resource: {Resource}", typeof(TEntity));
                await onError.WaitOnException(cause);
            }
        }
    }

    private async Task OnEventAsync(WatchEventType type, TEntity? entity, CancellationToken cancellationToken)
    {
        switch (type)
        {
            case WatchEventType.Added:
                _entityCache.TryAdd(entity.Uid(), entity.Generation() ?? 0);
                await ReconcileModificationAsync(entity!, cancellationToken);
                break;
            case WatchEventType.Modified:
                switch (entity)
                {
                    case { Metadata.DeletionTimestamp: null }:
                        _entityCache.TryGetValue(entity.Uid(), out var cachedGeneration);

                        // Check if entity spec has changed through "Generation" value increment. Skip reconcile if not changed.
                        if (entity.Generation() <= cachedGeneration)
                        {
                            logger.LogDebug(
                                """Entity "{Kind}/{Name}" modification did not modify generation. Skip event.""",
                                entity.Kind,
                                entity.Name());
                            return;
                        }

                        // update cached generation since generation now changed
                        _entityCache.TryUpdate(entity.Uid(), entity.Generation() ?? 1, cachedGeneration);
                        await ReconcileModificationAsync(entity, cancellationToken);
                        break;
                    case { Metadata: { DeletionTimestamp: not null, Finalizers.Count: > 0 } }:
                        await ReconcileFinalizersSequentialAsync(entity, cancellationToken);
                        break;
                }

                break;
            case WatchEventType.Deleted:
                await ReconcileDeletionAsync(entity!, cancellationToken);
                break;
            default:
                logger.LogWarning(
                    """Received unsupported event "{EventType}" for "{Kind}/{Name}".""",
                    type,
                    entity?.Kind,
                    entity.Name());
                break;
        }
    }

    private async Task ReconcileDeletionAsync(TEntity entity, CancellationToken cancellationToken)
    {
        requeue.Remove(entity);
        _entityCache.TryRemove(entity.Uid(), out _);

        await using var scope = provider.CreateAsyncScope();
        var controller = scope.ServiceProvider.GetRequiredService<IEntityController<TEntity>>();
        await controller.DeletedAsync(entity, cancellationToken);
    }

    private async Task ReconcileFinalizersSequentialAsync(TEntity entity, CancellationToken cancellationToken)
    {
        requeue.Remove(entity);
        await using var scope = provider.CreateAsyncScope();

        var identifier = entity.Finalizers().FirstOrDefault();
        if (identifier is null)
        {
            return;
        }

        if (scope.ServiceProvider.GetKeyedService<IEntityFinalizer<TEntity>>(identifier) is not
            { } finalizer)
        {
            logger.LogDebug(
                """Entity "{Kind}/{Name}" is finalizing but this operator has no registered finalizers for the identifier {FinalizerIdentifier}.""",
                entity.Kind,
                entity.Name(),
                identifier);
            return;
        }

        await finalizer.FinalizeAsync(entity, cancellationToken);
        entity.RemoveFinalizer(identifier);
        await client.UpdateAsync(entity, cancellationToken);
        logger.LogInformation(
            """Entity "{Kind}/{Name}" finalized with "{Finalizer}".""",
            entity.Kind,
            entity.Name(),
            identifier);
    }

    private async Task ReconcileModificationAsync(TEntity entity, CancellationToken cancellationToken)
    {
        // Re-queue should requested in the controller reconcile method. Invalidate any existing queues.
        requeue.Remove(entity);
        await using var scope = provider.CreateAsyncScope();
        var controller = scope.ServiceProvider.GetRequiredService<IEntityController<TEntity>>();
        await controller.ReconcileAsync(entity, cancellationToken);
    }
}
