using k8s;
using k8s.Models;

namespace KubeOps.Operator.Caching;

internal interface IResourceCache<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    TEntity Get(string id);

    TEntity Upsert(TEntity resource, out CacheComparisonResult result);

    bool Exists(TEntity resource);

    void Fill(IEnumerable<TEntity> resources);

    void Remove(TEntity resource);

    void Clear();
}
