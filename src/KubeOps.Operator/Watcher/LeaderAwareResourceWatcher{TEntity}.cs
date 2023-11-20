using k8s;
using k8s.LeaderElection;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Operator.Queue;

using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher;

internal class LeaderAwareResourceWatcher<TEntity>(ILogger<LeaderAwareResourceWatcher<TEntity>> logger,
        IServiceProvider provider,
        TimedEntityQueue<TEntity> queue,
        OperatorSettings settings,
        LeaderElector elector)
    : ResourceWatcher<TEntity>(logger, provider, queue, settings)
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Subscribe for leadership updates.");
        elector.OnStartedLeading += StartedLeading;
        elector.OnStoppedLeading += StoppedLeading;
        if (elector.IsLeader())
        {
            StartedLeading();
        }

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Unsubscribe from leadership updates.");
        elector.OnStartedLeading -= StartedLeading;
        elector.OnStoppedLeading -= StoppedLeading;
        return Task.CompletedTask;
    }

    private void StartedLeading()
    {
        logger.LogInformation("This instance started leading, starting watcher.");
        base.StartAsync(default);
    }

    private void StoppedLeading()
    {
        logger.LogInformation("This instance stopped leading, stopping watcher.");
        base.StopAsync(default);
    }
}
