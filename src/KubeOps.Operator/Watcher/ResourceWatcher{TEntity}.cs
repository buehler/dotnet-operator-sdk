using System.Collections.Concurrent;
using System.Net;
using System.Runtime.Serialization;
using System.Text.Json;

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
    : IHostedService
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly ConcurrentDictionary<string, long> _entityCache = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private uint _watcherReconnectRetries;
    private Task _eventWatcher = Task.CompletedTask;

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
#if NET8_0_OR_GREATER
        await _cancellationTokenSource.CancelAsync();
#else
        _cancellationTokenSource.Cancel();
#endif
        await _eventWatcher.WaitAsync(cancellationToken);
        _cancellationTokenSource.Dispose();
        logger.LogInformation("Stopped resource watcher for {ResourceType}.", typeof(TEntity).Name);
    }

    private async Task WatchClientEventsAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await foreach ((WatchEventType type, TEntity? entity) in client.WatchAsync<TEntity>(
                                   settings.Namespace,
                                   cancellationToken: stoppingToken))
                {
#pragma warning disable SA1312
                    using var _ = logger.BeginScope(new
#pragma warning restore SA1312
                    {
                        EventType = type,

                        // ReSharper disable once RedundantAnonymousTypePropertyName
                        Kind = entity?.Kind,
                        Name = entity?.Name(),
                        ResourceVersion = entity?.ResourceVersion(),
                    });
                    logger.LogInformation(
                        """Received watch event "{EventType}" for "{Kind}/{Name}", last observed resource version: {ResourceVersion}.""",
                        type,
                        entity?.Kind,
                        entity?.Name(),
                        entity?.ResourceVersion());
                    try
                    {
                        await OnEventAsync(type, entity, stoppingToken);
                    }
                    catch (KubernetesException e)
                    {
                        if (e.Status.Code == (int)HttpStatusCode.Gone)
                        {
                            logger.LogDebug("Watch restarting due to 410 HTTP Gone");

                            break;
                        }

                        LogReconciliationFailed(e);
                    }
                    catch (Exception e)
                    {
                        LogReconciliationFailed(e);
                    }

                    void LogReconciliationFailed(Exception exception)
                    {
                        logger.LogError(
                            exception,
                            "Reconciliation of {EventType} for {Kind}/{Name} failed.",
                            type,
                            entity?.Kind,
                            entity.Name());
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Don't throw if the cancellation was indeed requested.
        }
        catch (Exception e)
        {
            await OnWatchErrorAsync(e);
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
                                """Entity "{kind}/{name}" modification did not modify generation. Skip event.""",
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
                    """Received unsupported event "{eventType}" for "{kind}/{name}".""",
                    type,
                    entity?.Kind,
                    entity.Name());
                break;
        }
    }

    private async Task OnWatchErrorAsync(Exception e)
    {
        switch (e)
        {
            case SerializationException when
                e.InnerException is JsonException &&
                e.InnerException.Message.Contains("The input does not contain any JSON tokens"):
                logger.LogDebug(
                    """The watcher received an empty response for resource "{resource}".""",
                    typeof(TEntity));
                return;

            case HttpRequestException when
                e.InnerException is EndOfStreamException &&
                e.InnerException.Message.Contains("Attempted to read past the end of the stream."):
                logger.LogDebug(
                    """The watcher received a known error from the watched resource "{resource}". This indicates that there are no instances of this resource.""",
                    typeof(TEntity));
                return;
        }

        logger.LogError(e, """There was an error while watching the resource "{resource}".""", typeof(TEntity));
        _watcherReconnectRetries++;

        var delay = TimeSpan
            .FromSeconds(Math.Pow(2, Math.Clamp(_watcherReconnectRetries, 0, 5)))
            .Add(TimeSpan.FromMilliseconds(new Random().Next(0, 1000)));
        logger.LogWarning(
            "There were {retries} errors / retries in the watcher. Wait {seconds}s before next attempt to connect.",
            _watcherReconnectRetries,
            delay.TotalSeconds);
        await Task.Delay(delay);
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
                """Entity "{kind}/{name}" is finalizing but this operator has no registered finalizers for the identifier {finalizerIdentifier}.""",
                entity.Kind,
                entity.Name(),
                identifier);
            return;
        }

        await finalizer.FinalizeAsync(entity, cancellationToken);
        entity.RemoveFinalizer(identifier);
        await client.UpdateAsync(entity, cancellationToken);
        logger.LogInformation(
            """Entity "{kind}/{name}" finalized with "{finalizer}".""",
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
