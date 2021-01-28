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
    internal class ResourceCache<TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private const string ResourceVersion = "ResourceVersion";
        private const string ManagedFields = "ManagedFields";
        private const string Finalizers = "Metadata.Finalizers";
        private const string Status = "Status";

        private readonly CompareLogic _compare = new(
            new()
            {
                Caching = true,
                AutoClearCache = false,
                MembersToIgnore = new() { ResourceVersion, ManagedFields },
            });

        private readonly ConcurrentDictionary<string, TResource> _cache = new();

        private readonly ResourceCacheMetrics<TResource> _metrics;

        public ResourceCache(ResourceCacheMetrics<TResource> metrics)
        {
            _metrics = metrics;
        }

        public TResource Get(string id) => _cache[id];

        public TResource Upsert(TResource resource, out CacheComparisonResult result)
        {
            result = CompareCache(resource);

            var clone = resource.DeepClone();
            _cache.AddOrUpdate(resource.Metadata.Uid, clone, (_, _) => clone);

            _metrics.CachedItemsSize.Set(_cache.Count);
            _metrics.CachedItemsSummary.Observe(_cache.Count);
            return resource;
        }

        public void Fill(IEnumerable<TResource> resources)
        {
            foreach (var entity in resources)
            {
                var clone = entity.DeepClone();
                _cache.AddOrUpdate(entity.Metadata.Uid, clone, (_, _) => clone);
            }

            _metrics.CachedItemsSize.Set(_cache.Count);
            _metrics.CachedItemsSummary.Observe(_cache.Count);
        }

        public void Remove(TResource resource) => Remove(resource.Metadata.Uid);

        public void Clear()
        {
            _cache.Clear();
            _metrics.CachedItemsSize.Set(_cache.Count);
            _metrics.CachedItemsSummary.Observe(_cache.Count);
        }

        private CacheComparisonResult CompareCache(TResource resource)
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

        private bool Exists(TResource resource) => _cache.ContainsKey(resource.Metadata.Uid);

        private void Remove(string resourceUid)
        {
            _cache.TryRemove(resourceUid, out _);
            _metrics.CachedItemsSize.Set(_cache.Count);
            _metrics.CachedItemsSummary.Observe(_cache.Count);
        }
    }
}
