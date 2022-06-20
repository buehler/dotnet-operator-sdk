using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KubeOps.Operator.Leadership;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Controller;

internal class ResourceControllerManager : IHostedService
{
    private readonly IControllerInstanceBuilder _controllerInstanceBuilder;
    private readonly ILeaderElection _leaderElection;
    private readonly OperatorSettings _operatorSettings;
    private readonly List<ScopedResourceController> _controllerList;

    private IDisposable? _leadershipSubscription;

    public ResourceControllerManager(
        IControllerInstanceBuilder controllerInstanceBuilder,
        ILeaderElection leaderElection,
        OperatorSettings operatorSettings)
    {
        _controllerInstanceBuilder = controllerInstanceBuilder;
        _leaderElection = leaderElection;
        _operatorSettings = operatorSettings;
        _controllerList = new List<ScopedResourceController>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _controllerList.AddRange(_controllerInstanceBuilder.BuildControllers());

        _leadershipSubscription = _leaderElection.LeadershipChange.Subscribe(LeadershipChanged);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _leadershipSubscription?.Dispose();
        foreach (var scopedController in _controllerList)
        {
            scopedController.StopAsync();
            scopedController.Dispose();
        }

        _controllerList.Clear();
        return Task.CompletedTask;
    }

    private void LeadershipChanged(LeaderState state)
    {
        if (state == LeaderState.None)
        {
            return;
        }

        foreach (var scopedController in _controllerList)
        {
            if (state == LeaderState.Leader
                || !_operatorSettings.OnlyWatchEventsWhenLeader)
            {
                scopedController.StartAsync();
            }
            else
            {
                scopedController.StopAsync();
            }
        }
    }
}
