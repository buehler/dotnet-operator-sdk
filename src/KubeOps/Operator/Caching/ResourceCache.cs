using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using k8s;
using k8s.Models;
using KellermanSoftware.CompareNetObjects;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Entities.Extensions;

namespace KubeOps.Operator.Caching
{
    internal class ResourceCache<TEntity> : IResourceCache<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private const string ResourceVersion = "ResourceVersion";
        private const string ManagedFields = "ManagedFields";
        private const string Finalizers = "Metadata.Finalizers";
        private const string Status = "Status";

        private readonly CompareLogic _compare = new(
            new ComparisonConfig
            {
                Caching = true,
                AutoClearCache = false,
                MembersToIgnore = new List<string> { ResourceVersion, ManagedFields },
            });

        private readonly IDictionary<string, TEntity> _cache = new ConcurrentDictionary<string, TEntity>();

        private readonly ResourceCacheMetrics<TEntity> _metrics;

        public ResourceCache(OperatorSettings settings)
        {
            _metrics = new ResourceCacheMetrics<TEntity>(settings);
        }

        public TEntity Get(string id) => _cache[id];

        public TEntity Upsert(TEntity resource, out CacheComparisonResult result)
        {
            result = CompareCache(resource);

            if (result == CacheComparisonResult.New)
            {
                _cache.Add(resource.Metadata.Uid, resource.DeepClone());
            }
            else
            {
                _cache[resource.Metadata.Uid] = resource.DeepClone();
            }

            _metrics.CachedItemsSize.Set(_cache.Count);
            _metrics.CachedItemsSummary.Observe(_cache.Count);
            return resource;
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
                return CacheComparisonResult.New;
            }

            var cacheObject = _cache[resource.Metadata.Uid];
            var comparison = _compare.Compare(resource, cacheObject);
            if (comparison.AreEqual)
            {
                return CacheComparisonResult.NotModified;
            }

            if (comparison.Differences.All(d => d.PropertyName.Split('.')[0] == Status))
            {
                return CacheComparisonResult.StatusModified;
            }

            if (comparison.Differences.All(d => d.ParentPropertyName == Finalizers || d.PropertyName == Finalizers))
            {
                return CacheComparisonResult.FinalizersModified;
            }

            return CacheComparisonResult.Modified;
        }

        private bool Exists(TEntity resource) => _cache.ContainsKey(resource.Metadata.Uid);

        private void Remove(string resourceUid)
        {
            _cache.Remove(resourceUid);
            _metrics.CachedItemsSize.Set(_cache.Count);
            _metrics.CachedItemsSummary.Observe(_cache.Count);
        }
    }
}
