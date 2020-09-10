using System;

namespace KubeOps.Operator.Leadership
{
    public interface ILeaderElection
    {
        event EventHandler<LeaderState>? LeadershipChange;

        LeaderState State { get; }

        internal void LeadershipChanged(LeaderState state);
    }
}
