using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;

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
    /// Add metadata for an entity to the operator.
    /// Metadata must be added for each entity to be used in
    /// controllers and other elements.
    /// </summary>
    /// <param name="metadata">The metadata of the entity.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddEntityMetadata<TEntity>(EntityMetadata metadata)
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
    /// Add a controller implementation for a specific entity with the
    /// entity metadata.
    /// </summary>
    /// <param name="metadata">The metadata of the entity.</param>
    /// <typeparam name="TImplementation">Implementation type of the controller.</typeparam>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddController<TImplementation, TEntity>(EntityMetadata metadata)
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
