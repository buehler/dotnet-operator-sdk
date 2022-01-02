namespace KubeOps.Operator.Caching;

/// <summary>
/// Result for the <see cref="ResourceCache{TEntity}.Upsert"/> when comparison is done.
/// </summary>
internal enum CacheComparisonResult
{
    /// <summary>
    /// <para>
    /// The resource is either:
    /// <list type="bullet">
    /// <item>
    /// <term>New to the cache</term>
    /// </item>
    /// <item>
    /// <term>Modified</term>
    /// </item>
    /// <item>
    /// <term>Not Modified</term>
    /// </item>
    /// </list>
    /// </para>
    /// <para>But not status or finalizer modified. This is used to reconcile objects.</para>
    /// </summary>
    Other,

    /// <summary>
    /// The resource has changed, but only the "status" of it.
    /// </summary>
    StatusModified,

    /// <summary>
    /// The resource has changed, but only the "finalizers" list.
    /// </summary>
    FinalizersModified,
}
