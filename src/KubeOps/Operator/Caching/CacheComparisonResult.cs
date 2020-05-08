namespace Dos.Operator.Caching
{
    internal enum CacheComparisonResult
    {
        New,
        Modified,
        StatusModified,
        FinalizersModified,
        NotModified,
    }
}
