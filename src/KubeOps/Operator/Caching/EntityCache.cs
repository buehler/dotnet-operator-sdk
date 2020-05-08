using System.Collections.Generic;
using System.Linq;
using k8s;
using k8s.Models;
using KellermanSoftware.CompareNetObjects;
using KubeOps.Operator.Entities;

namespace KubeOps.Operator.Caching
{
    internal class EntityCache<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private const string ResourceVersion = "ResourceVersion";
        private const string Finalizers = "Metadata.Finalizers";
        private const string Status = "Status";

        private readonly CompareLogic _compare = new CompareLogic(
            new ComparisonConfig
            {
                Caching = true,
                AutoClearCache = false,
                MembersToIgnore = new List<string> { ResourceVersion },
            });

        private readonly IDictionary<string, TEntity> _cache = new Dictionary<string, TEntity>();

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

            return resource;
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

            if (comparison.Differences.All(d => d.ParentPropertyName == Status))
            {
                return CacheComparisonResult.StatusModified;
            }

            if (comparison.Differences.All(d => d.PropertyName == Finalizers))
            {
                return CacheComparisonResult.FinalizersModified;
            }

            return CacheComparisonResult.Modified;
        }

        public void Remove(TEntity resource) => Remove(resource.Metadata.Uid);

        public void Clear() => _cache.Clear();

        private bool Exists(TEntity resource) => _cache.ContainsKey(resource.Metadata.Uid);

        private bool Exists(string id) => _cache.ContainsKey(id);

        private void Remove(string resourceUid) => _cache.Remove(resourceUid);
    }
}
