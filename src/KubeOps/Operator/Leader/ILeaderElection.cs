using System;

namespace KubeOps.Operator.Leader
{
    public interface ILeaderElection
    {
        event EventHandler<LeaderState>? LeadershipChange;

        LeaderState State { get; }

        internal void LeadershipChanged(LeaderState state);
    }
}
