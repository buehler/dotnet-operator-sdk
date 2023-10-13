using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
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
    /// <para>
    /// Register an entity Kubernetes client within the operator.
    /// This is used to register IKubernetesClient{TEntity} for the entity.
    /// An alternative way to create any Kubernetes client is to use the
    /// KubernetesClientFactory or instantiate the client by yourself.
    /// </para>
    /// </summary>
    /// <param name="metadata">The metadata of the entity.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddEntityClient<TEntity>(EntityMetadata metadata)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// <para>
    /// Register an entity Kubernetes client within the operator.
    /// This is used to register IKubernetesClient{TEntity} for the entity.
    /// An alternative way to create any Kubernetes client is to use the
    /// KubernetesClientFactory or instantiate the client by yourself.
    /// This method uses reflection to get the metadata from the entity.
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddEntityClient<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>;

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
