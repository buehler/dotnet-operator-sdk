using System.Collections.Concurrent;
using k8s;
using k8s.Models;
using KellermanSoftware.CompareNetObjects;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Entities.Extensions;

namespace KubeOps.Operator.Caching;

internal class ResourceCache<TEntity> : IResourceCache<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private const string Finalizers = "Metadata.Finalizers";
    private const string Status = "Status";

    private readonly CompareLogic _compare;

    private readonly ConcurrentDictionary<string, TEntity> _cache = new();

    private readonly ResourceCacheMetrics<TEntity> _metrics;

    public ResourceCache(ResourceCacheMetrics<TEntity> metrics, OperatorSettings settings)
    {
        _metrics = metrics;

        _compare = new CompareLogic(settings.CacheComparisonConfig);
    }

    public TEntity Get(string id) => _cache[id];

    public TEntity Upsert(TEntity resource, out CacheComparisonResult result)
    {
        result = CompareCache(resource);

        var clone = resource.DeepClone();
        if (clone == null)
        {
            throw new ArgumentNullException(nameof(clone));
        }

        _cache.AddOrUpdate(resource.Metadata.Uid, clone, (_, _) => clone);

        _metrics.CachedItemsSize.Set(_cache.Count);
        _metrics.CachedItemsSummary.Observe(_cache.Count);
        return resource;
    }

    public void Fill(IEnumerable<TEntity> resources)
    {
        foreach (var entity in resources)
        {
            var clone = entity.DeepClone();
            if (clone == null)
            {
                throw new ArgumentNullException(nameof(clone));
            }

            _cache.AddOrUpdate(entity.Metadata.Uid, clone, (_, _) => clone);
        }

        _metrics.CachedItemsSize.Set(_cache.Count);
        _metrics.CachedItemsSummary.Observe(_cache.Count);
    }

    public void Remove(TEntity resource) => Remove(resource.Metadata.Uid);

    public void Clear()
    {
        _cache.Clear();
        _metrics.CachedItemsSize.Set(_cache.Count);
        _metrics.CachedItemsSummary.Observe(_cache.Count);
    }

    private CacheComparisonResult CompareCache(TEntity resource)
    {
        if (!Exists(resource))
        {
            return CacheComparisonResult.Other;
        }

        var cacheObject = _cache[resource.Metadata.Uid];
        var comparison = _compare.Compare(resource, cacheObject);
        if (comparison.AreEqual)
        {
            return CacheComparisonResult.Other;
        }

        if (comparison.Differences.All(d => d.PropertyName.Split('.')[0] == Status))
        {
            return CacheComparisonResult.StatusModified;
        }

        if (comparison.Differences.All(d => d.ParentPropertyName == Finalizers || d.PropertyName == Finalizers))
        {
            return CacheComparisonResult.FinalizersModified;
        }

        return CacheComparisonResult.Other;
    }

    private bool Exists(TEntity resource) => _cache.ContainsKey(resource.Metadata.Uid);

    private void Remove(string resourceUid)
    {
        _cache.TryRemove(resourceUid, out _);
        _metrics.CachedItemsSize.Set(_cache.Count);
        _metrics.CachedItemsSummary.Observe(_cache.Count);
    }
}
