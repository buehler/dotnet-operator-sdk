using System;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Leadership
{
    internal class LeaderElection : ILeaderElection
    {
        private readonly ILogger<LeaderElection> _logger;

        public LeaderElection(ILogger<LeaderElection> logger)
        {
            _logger = logger;
        }

        public event EventHandler<LeaderState>? LeadershipChange;

        public LeaderState State { get; private set; } = LeaderState.None;

        void ILeaderElection.LeadershipChanged(LeaderState state)
        {
            if (State == state)
            {
                return;
            }

            _logger.LogDebug("Leadership state changed to: {state}.", state);
            State = state;
            LeadershipChange?.Invoke(this, state);
        }
    }
}
