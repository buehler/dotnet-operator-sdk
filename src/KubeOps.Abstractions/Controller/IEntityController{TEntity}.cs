using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Controller;

public interface IEntityController<in TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
#if NETSTANDARD2_0
    Task ReconcileAsync(TEntity entity);

    Task DeletedAsync(TEntity entity);
#else
    Task ReconcileAsync(TEntity entity) =>
        Task.CompletedTask;

    Task DeletedAsync(TEntity entity) =>
        Task.CompletedTask;
#endif
}
