namespace KubeOps.Operator.Leadership;

/// <summary>
/// Leader elector for the operator.
/// </summary>
public interface ILeaderElection
{
    /// <summary>
    /// Event that is fired when the leadership state changes.
    /// </summary>
    IObservable<LeaderState> LeadershipChange { get; }

    internal void LeadershipChanged(LeaderState state);
}
