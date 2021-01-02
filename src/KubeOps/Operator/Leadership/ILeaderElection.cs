using System;

namespace KubeOps.Operator.Leadership
{
    /// <summary>
    /// Leader elector for the operator.
    /// </summary>
    public interface ILeaderElection
    {
        /// <summary>
        /// Event that is fired when the leadership state changes.
        /// </summary>
        event EventHandler<LeaderState>? LeadershipChange;

        /// <summary>
        /// The current state.
        /// </summary>
        LeaderState State { get; }

        internal void LeadershipChanged(LeaderState state);
    }
}
