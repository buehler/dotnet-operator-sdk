namespace KubeOps.Operator.Caching
{
    public enum CacheComparisonResult
    {
        New,
        Modified,
        StatusModified,
        FinalizersModified,
        NotModified,
    }
}
