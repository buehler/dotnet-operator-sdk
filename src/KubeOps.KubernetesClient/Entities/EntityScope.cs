namespace KubeOps.KubernetesClient.Entities;

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
