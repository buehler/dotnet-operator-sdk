using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Controller;

/// <summary>
/// Generic entity controller interface.
/// </summary>
/// <typeparam name="TEntity">The type of the Kubernetes entity.</typeparam>
public interface IEntityController<in TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    /// <summary>
    /// Called for `added` and `modified` events from the watcher.
    /// </summary>
    /// <param name="entity">The entity that fired the reconcile event.</param>
    /// <returns>A task that completes when the reconciliation is done.</returns>
#if NETSTANDARD2_0
    Task ReconcileAsync(TEntity entity);
#else
    Task ReconcileAsync(TEntity entity) =>
        Task.CompletedTask;
#endif

    /// <summary>
    /// Called for `delete` events for a given entity.
    /// </summary>
    /// <param name="entity">The entity that fired the deleted event.</param>
    /// <returns>
    /// A task that completes, when the reconciliation is done.
    /// </returns>
#if NETSTANDARD2_0
    Task DeletedAsync(TEntity entity);
#else
    Task DeletedAsync(TEntity entity) =>
        Task.CompletedTask;
#endif
}
