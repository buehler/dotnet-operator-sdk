using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer;

internal interface IFinalizerInstanceBuilder
{
    public IEnumerable<IResourceFinalizer<TEntity>> BuildFinalizers<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    IResourceFinalizer<TEntity> BuildFinalizer<TEntity, TFinalizer>()
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
