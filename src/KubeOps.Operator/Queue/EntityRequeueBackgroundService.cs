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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Return back to the caller, so that the rest of the host can startup.
        await Task.Yield();

        // Do not await for this, otherwise the host won't start.
        _ = WatchAsync(_cts.Token);
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
