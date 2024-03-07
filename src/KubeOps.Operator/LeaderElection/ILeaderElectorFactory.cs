using k8s.LeaderElection;

namespace KubeOps.Operator.LeaderElection;

/// <summary>
/// Represents a type used to configure the election system and create instances of <see cref="LeaderElector" />.
/// </summary>
public interface ILeaderElectorFactory
{
    /// <summary>
    /// Creates a new <see cref="LeaderElector"/> instance.
    /// </summary>
    /// <returns>The <see cref="LeaderElector"/>.</returns>
    LeaderElector CreateElector();
}
