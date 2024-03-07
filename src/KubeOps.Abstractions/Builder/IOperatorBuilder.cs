using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Abstractions.Builder;

/// <summary>
/// KubeOps operator builder.
/// </summary>
public interface IOperatorBuilder
{
    /// <summary>
    /// The original service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Add a controller implementation for a specific entity to the operator.
    /// The metadata for the entity must be added as well.
    /// </summary>
    /// <typeparam name="TImplementation">Implementation type of the controller.</typeparam>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddController<TImplementation, TEntity>()
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Add a finalizer implementation for a specific entity.
    /// This adds the implementation as a transient service and registers
    /// the finalizer with the provided identifier. Then an
    /// <see cref="EntityFinalizerAttacher{TImplementation,TEntity}"/> is registered to
    /// provide a delegate for attaching the finalizer to an entity.
    /// </summary>
    /// <param name="identifier">
    /// The identifier for the finalizer.
    /// This string is added to the Kubernetes entity as a finalizer.
    /// </param>
    /// <typeparam name="TImplementation">Type of the finalizer implementation.</typeparam>
    /// <typeparam name="TEntity">Type of the Kubernetes entity.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddFinalizer<TImplementation, TEntity>(string identifier)
        where TImplementation : class, IEntityFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
