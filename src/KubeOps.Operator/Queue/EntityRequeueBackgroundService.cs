﻿using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Queue;

internal sealed class EntityRequeueBackgroundService<TEntity>(
    IKubernetesClient client,
    TimedEntityQueue<TEntity> queue,
    IServiceProvider provider,
    ILogger<EntityRequeueBackgroundService<TEntity>> logger) : IHostedService, IDisposable, IAsyncDisposable
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // The current implementation of IHostedService expects that StartAsync is "really" asynchronous.
        // Blocking calls are not allowed, they would stop the rest of the startup flow.
        //
        // This is an open issue since 2019 and not expected to be closed soon. (https://github.com/dotnet/runtime/issues/36063)
        // For reasons unknown at the time of writing this code, "await Task.Yield()" didn't work as expected, it caused
        // a deadlock in 1/10 of the cases.
        //
        // Therefore, we use Task.Run() and put the work to queue. The passed cancellation token of the StartAsync
        // method is not used, because it would only cancel the scheduling (which we definitely don't want to cancel).
        // To make this intention explicit, CancellationToken.None gets passed.
        _ = Task.Run(() => WatchAsync(_cts.Token), CancellationToken.None);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

#if NET8_0_OR_GREATER
        return _cts.CancelAsync();
#else
        _cts.Cancel();
        return Task.CompletedTask;
#endif
    }

    public void Dispose()
    {
        _cts.Dispose();
        client.Dispose();
        queue.Dispose();

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_cts);
        await CastAndDispose(client);
        await CastAndDispose(queue);

        _disposed = true;

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

    private async Task WatchAsync(CancellationToken cancellationToken)
    {
        await foreach (var entity in queue)
        {
            try
            {
                await ReconcileSingleAsync(entity, cancellationToken);
            }
            catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(
                    e,
                    """Queued reconciliation for the entity of type {ResourceType} for "{Kind}/{Name}" failed.""",
                    typeof(TEntity).Name,
                    entity.Kind,
                    entity.Name());
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    """Queued reconciliation for the entity of type {ResourceType} for "{Kind}/{Name}" failed.""",
                    typeof(TEntity).Name,
                    entity.Kind,
                    entity.Name());
            }
        }
    }

    private async Task ReconcileSingleAsync(TEntity queued, CancellationToken cancellationToken)
    {
        logger.LogTrace("""Execute requested requeued reconciliation for "{Name}".""", queued.Name());

        if (await client.GetAsync<TEntity>(queued.Name(), queued.Namespace(), cancellationToken) is not
            { } entity)
        {
            logger.LogWarning(
                """Requeued entity "{Name}" was not found. Skipping reconciliation.""", queued.Name());
            return;
        }

        await using var scope = provider.CreateAsyncScope();
        var controller = scope.ServiceProvider.GetRequiredService<IEntityController<TEntity>>();
        await controller.ReconcileAsync(entity, cancellationToken);
    }
}
