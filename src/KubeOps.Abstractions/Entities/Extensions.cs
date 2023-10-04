using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

/// <summary>
/// Method extensions for <see cref="IKubernetesObject{TMetadata}"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Sets the resource version of the specified Kubernetes object to the specified value.
    /// </summary>
    /// <typeparam name="TEntity">The type of the Kubernetes object.</typeparam>
    /// <param name="entity">The Kubernetes object.</param>
    /// <param name="resourceVersion">The resource version to set.</param>
    /// <returns>The Kubernetes object with the updated resource version.</returns>
    public static TEntity WithResourceVersion<TEntity>(
        this TEntity entity,
        string resourceVersion)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        entity.EnsureMetadata().ResourceVersion = resourceVersion;
        return entity;
    }

    /// <summary>
    /// Sets the resource version of the specified Kubernetes object to the resource version of another object.
    /// </summary>
    /// <typeparam name="TEntity">The type of the Kubernetes object.</typeparam>
    /// <param name="entity">The Kubernetes object.</param>
    /// <param name="other">The other Kubernetes object.</param>
    /// <returns>The Kubernetes object with the updated resource version.</returns>
    public static TEntity WithResourceVersion<TEntity>(
        this TEntity entity,
        TEntity other)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        entity.EnsureMetadata().ResourceVersion = other.ResourceVersion();
        return entity;
    }

    /// <summary>
    /// Create a <see cref="V1ObjectReference"/> of a kubernetes object.
    /// </summary>
    /// <param name="kubernetesObject">The object that should be translated.</param>
    /// <returns>The created <see cref="V1ObjectReference"/>.</returns>
    public static V1ObjectReference MakeObjectReference(this IKubernetesObject<V1ObjectMeta> kubernetesObject)
        => new()
        {
            ApiVersion = kubernetesObject.ApiVersion,
            Kind = kubernetesObject.Kind,
            Name = kubernetesObject.Metadata.Name,
            NamespaceProperty = kubernetesObject.Metadata.NamespaceProperty,
            ResourceVersion = kubernetesObject.Metadata.ResourceVersion,
            Uid = kubernetesObject.Metadata.Uid,
        };
}
