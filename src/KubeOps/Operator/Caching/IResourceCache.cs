using k8s;
using k8s.Models;

namespace KubeOps.Operator.Caching
{
    public interface IResourceCache<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        TEntity Get(string id);

        TEntity Upsert(TEntity resource, out CacheComparisonResult result);

        void Remove(TEntity resource);

        public void Clear();
    }
}
