using k8s;
using k8s.LeaderElection;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Queue;

using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher;

internal class LeaderAwareResourceWatcher<TEntity> : ResourceWatcher<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly ILogger<LeaderAwareResourceWatcher<TEntity>> _logger;
    private readonly LeaderElector _elector;

    public LeaderAwareResourceWatcher(
        ILogger<LeaderAwareResourceWatcher<TEntity>> logger,
        IServiceProvider provider,
        TimedEntityQueue<TEntity> queue,
        OperatorSettings settings,
        LeaderElector elector)
        : base(logger, provider, queue, settings)
    {
        _logger = logger;
        _elector = elector;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Subscribe for leadership updates.");
        _elector.OnStartedLeading += StartedLeading;
        _elector.OnStoppedLeading += StoppedLeading;
        if (_elector.IsLeader())
        {
            StartedLeading();
        }

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Unsubscribe from leadership updates.");
        _elector.OnStartedLeading -= StartedLeading;
        _elector.OnStoppedLeading -= StoppedLeading;
        return Task.CompletedTask;
    }

    private void StartedLeading()
    {
        _logger.LogInformation("This instance started leading, starting watcher.");
        base.StartAsync(default);
    }

    private void StoppedLeading()
    {
        _logger.LogInformation("This instance stopped leading, stopping watcher.");
        base.StopAsync(default);
    }
}
