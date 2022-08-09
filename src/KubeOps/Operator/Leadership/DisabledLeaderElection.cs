using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace KubeOps.Operator.Leadership;

internal class DisabledLeaderElection : ILeaderElection, IDisposable
{
    private readonly BehaviorSubject<LeaderState> _leadershipChange = new(LeaderState.Leader);

    public IObservable<LeaderState> LeadershipChange => _leadershipChange.DistinctUntilChanged();

    public void Dispose()
    {
        _leadershipChange.Dispose();
    }

    void ILeaderElection.LeadershipChanged(LeaderState state)
    {
    }
}
