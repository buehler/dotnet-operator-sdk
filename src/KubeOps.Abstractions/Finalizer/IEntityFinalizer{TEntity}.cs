using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Finalizer;

/// <summary>
/// Finalizer for an entity.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public interface IEntityFinalizer<in TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    /// <summary>
    /// Finalize an entity that is pending for deletion.
    /// </summary>
    /// <param name="entity">The kubernetes entity that needs to be finalized.</param>
    /// <returns>A task that resolves when the operation is done.</returns>
    Task FinalizeAsync(TEntity entity) =>
        Task.CompletedTask;
}
