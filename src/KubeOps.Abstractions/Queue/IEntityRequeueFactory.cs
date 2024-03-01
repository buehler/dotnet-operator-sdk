using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Queue;

/// <summary>
/// Represents a type used to create delegates of type <see cref="EntityRequeue{TEntity}"/> for requeueing entities.
/// </summary>
public interface IEntityRequeueFactory
{
    /// <summary>
    /// Creates a new <see cref="EntityRequeue{TEntity}"/> for the given <typeparamref name="TEntity"/> type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A <see cref="EntityRequeue{TEntity}"/>.</returns>
    EntityRequeue<TEntity> Create<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
