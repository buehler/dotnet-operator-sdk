using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace KubeOps.Operator.Leadership;

internal class LeaderElection : ILeaderElection, IDisposable
{
    private readonly ILogger<LeaderElection> _logger;
    private readonly BehaviorSubject<LeaderState> _leadershipChange = new(LeaderState.None);

    public LeaderElection(ILogger<LeaderElection> logger)
    {
        _logger = logger;
    }

    public IObservable<LeaderState> LeadershipChange => _leadershipChange.DistinctUntilChanged();

    public void Dispose()
    {
        _leadershipChange.Dispose();
    }

    void ILeaderElection.LeadershipChanged(LeaderState state)
    {
        _logger.LogDebug("Leadership state changed to: {state}.", state);
        _leadershipChange.OnNext(state);
    }
}
