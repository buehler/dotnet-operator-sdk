using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Finalizer;

public delegate Task<TEntity> EntityFinalizerAttacher<TImplementation, TEntity>(TEntity entity)
    where TImplementation : IEntityFinalizer<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>;
