namespace KubeOps.Operator.Caching
{
    public enum CacheComparisonResult
    {
        /// <summary>
        /// The resource is new to the cache
        /// and was never seen.
        /// </summary>
        New,

        /// <summary>
        /// The resource was in the cache and some
        /// properties changed (but not resourceVersion).
        /// </summary>
        Modified,

        /// <summary>
        /// The resource has changed, but only the "status" of it.
        /// </summary>
        StatusModified,

        /// <summary>
        /// The resource has changed, but only the "finalizers" list.
        /// </summary>
        FinalizersModified,

        /// <summary>
        /// The resource stayed the same.
        /// </summary>
        NotModified,
    }
}
