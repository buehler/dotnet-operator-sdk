using System.Reactive.Subjects;
using KubeOps.Operator.Leadership;

namespace KubeOps.Testing;

internal class MockedLeaderElection : ILeaderElection
{
    public IObservable<LeaderState> LeadershipChange { get; } =
        new BehaviorSubject<LeaderState>(LeaderState.Leader);

    void ILeaderElection.LeadershipChanged(LeaderState state)
    {
    }
}
