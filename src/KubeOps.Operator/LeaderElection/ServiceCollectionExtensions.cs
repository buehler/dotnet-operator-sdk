using k8s.LeaderElection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KubeOps.Operator.LeaderElection;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds support for leader election.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddLeaderElection(this IServiceCollection services)
    {
        // In order to enable leader election, two things need to be done:
        // First, we need to setup the LeaderElector, which is done by the factory. This is done in order to allow the
        // injection of other services (like the k8s.IKubernetes) into the creation of the elector.
        services.TryAddSingleton<ILeaderElectorFactory, KubernetesLeaderElectorFactory>();
        services.TryAddSingleton<LeaderElector>(provider =>
            provider.GetRequiredService<ILeaderElectorFactory>().CreateElector());

        // The second thing to do is the addition of the LeaderElectionBackgroundService which is responsible for managing
        // the leader election itself.
        services.AddHostedService<LeaderElectionBackgroundService>();
    }
}
