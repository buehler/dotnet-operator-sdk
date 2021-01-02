namespace KubeOps.Operator.Entities
{
    /// <summary>
    /// Definition for the scopes of entities.
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
}
