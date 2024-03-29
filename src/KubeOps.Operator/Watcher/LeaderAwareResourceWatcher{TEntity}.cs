﻿using k8s;
using k8s.LeaderElection;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Queue;

using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher;

internal sealed class LeaderAwareResourceWatcher<TEntity>(
    ILogger<LeaderAwareResourceWatcher<TEntity>> logger,
    IServiceProvider provider,
    TimedEntityQueue<TEntity> queue,
    OperatorSettings settings,
    IKubernetesClient client,
    LeaderElector elector)
    : ResourceWatcher<TEntity>(logger, provider, queue, settings, client)
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Subscribe for leadership updates.");

        elector.OnStartedLeading += StartedLeading;
        elector.OnStoppedLeading += StoppedLeading;

        return elector.IsLeader() ? base.StartAsync(_cts.Token) : Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Unsubscribe from leadership updates.");
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        elector.OnStartedLeading -= StartedLeading;
        elector.OnStoppedLeading -= StoppedLeading;
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _cts.Dispose();
        elector.Dispose();
        _disposed = true;

        base.Dispose(disposing);
    }

    private void StartedLeading()
    {
        logger.LogInformation("This instance started leading, starting watcher.");
        base.StartAsync(_cts.Token);
    }

    private void StoppedLeading()
    {
        _cts.Cancel();

        logger.LogInformation("This instance stopped leading, stopping watcher.");
        base.StopAsync(_cts.Token).Wait();
    }
}
