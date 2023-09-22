using KubeOps.Operator.Leadership;

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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _leadershipSubscription?.Dispose();
        foreach (var controller in _controllerList)
        {
            await controller.StopAsync();
            controller.Dispose();
        }

        _controllerList.Clear();
    }

    private async void LeadershipChanged(LeaderState state)
    {
        if (state == LeaderState.None)
        {
            return;
        }

        foreach (var controller in _controllerList)
        {
            if (state == LeaderState.Leader || !_operatorSettings.OnlyWatchEventsWhenLeader)
            {
                await controller.StartAsync();
            }
            else
            {
                await controller.StopAsync();
            }
        }
    }
}
