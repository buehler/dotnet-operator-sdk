using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.KubernetesClient;
using KubeOps.Transpiler;

namespace KubeOps.Operator.Client;

/// <summary>
/// Factory to create a <see cref="IKubernetesClient{TEntity}"/> for a given <see cref="EntityMetadata"/>
/// or type.
/// </summary>
public static class KubernetesClientFactory
{
    /// <summary>
    /// Create a <see cref="IKubernetesClient{TEntity}"/> for a given <see cref="EntityMetadata"/>.
    /// This method does not use reflection at runtime since the metadata is already provided.
    /// </summary>
    /// <param name="metadata">Metadata for the entity.</param>
    /// <typeparam name="TEntity">Type of the entity.</typeparam>
    /// <returns>The created <see cref="IKubernetesClient{TEntity}"/>.</returns>
    public static IKubernetesClient<TEntity> Create<TEntity>(EntityMetadata metadata)
        where TEntity : IKubernetesObject<V1ObjectMeta> =>
        new KubernetesClient<TEntity>(metadata);

    /// <summary>
    /// Create a <see cref="IKubernetesClient{TEntity}"/> for a given type.
    /// This method uses reflection at runtime to fetch the metadata for the entity.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity.</typeparam>
    /// <returns>The created <see cref="IKubernetesClient{TEntity}"/>.</returns>
    public static IKubernetesClient<TEntity> Create<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta> =>
        Create<TEntity>(Entities.ToEntityMetadata(typeof(TEntity)).Metadata);
}
