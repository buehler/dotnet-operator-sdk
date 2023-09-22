namespace KubeOps.Abstractions.Entities;

/// <summary>
/// Scope of the resource. Custom entities (resources) in Kubernetes
/// can either be namespaced or cluster-wide.
/// </summary>
public enum EntityScope
{
    /// <summary>
    /// The resource is namespace.
    /// </summary>
    Namespaced,

    /// <summary>
    /// The resource is cluster-wide.
    /// </summary>
    Cluster,
}
