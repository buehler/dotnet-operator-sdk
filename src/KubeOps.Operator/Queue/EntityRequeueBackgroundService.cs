using k8s;
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
    ILogger<EntityRequeueBackgroundService<TEntity>> logger) : IHostedService, IDisposable
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly CancellationTokenSource _cts = new();

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

#if NET8_0_OR_GREATER
    public Task StopAsync(CancellationToken cancellationToken) => _cts.CancelAsync();
#else
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }
#endif

    public void Dispose() => _cts.Dispose();

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
        logger.LogTrace("""Execute requested requeued reconciliation for "{name}".""", queued.Name());

        if (await client.GetAsync<TEntity>(queued.Name(), queued.Namespace(), cancellationToken) is not
            { } entity)
        {
            logger.LogWarning(
                """Requeued entity "{name}" was not found. Skipping reconciliation.""", queued.Name());
            return;
        }

        await using var scope = provider.CreateAsyncScope();
        var controller = scope.ServiceProvider.GetRequiredService<IEntityController<TEntity>>();
        await controller.ReconcileAsync(entity, cancellationToken);
    }
}
