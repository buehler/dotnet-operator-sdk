using k8s.LeaderElection;

using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.LeaderElection;

/// <summary>
/// This background service connects to the API and continuously watches the leader election.
/// </summary>
/// <param name="elector">The elector.</param>
internal sealed class LeaderElectionBackgroundService(LeaderElector elector) : BackgroundService
{
    public override void Dispose() => elector.Dispose();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yielding the task is necessary to prevent blocking the startup.
        // See also: https://github.com/dotnet/runtime/issues/36063
        await Task.Yield();
        await elector.RunAsync(stoppingToken);
    }
}
