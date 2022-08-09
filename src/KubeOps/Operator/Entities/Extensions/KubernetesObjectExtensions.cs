using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities.Extensions;

/// <summary>
/// Extensions for various kubernetes objects.
/// </summary>
public static class KubernetesObjectExtensions
{
    /// <summary>
    /// Ensures the object contains owner references and adds the owner to the list.
    /// </summary>
    /// <param name="resource">The resource that is owned by another resource.</param>
    /// <param name="owner">The owner to add.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <returns>The resource with the added owner reference.</returns>
    public static TEntity WithOwnerReference<TEntity>(
        this TEntity resource,
        IKubernetesObject<V1ObjectMeta> owner)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        resource.Metadata.EnsureOwnerReferences().Add(owner.MakeOwnerReference());
        return resource;
    }

    /// <summary>
    /// Create a <see cref="V1OwnerReference"/> out of a kubernetes object.
    /// </summary>
    /// <param name="kubernetesObject">The object that should be translated.</param>
    /// <returns>The created <see cref="V1OwnerReference"/>.</returns>
    public static V1OwnerReference MakeOwnerReference(this IKubernetesObject<V1ObjectMeta> kubernetesObject)
        => new(
            kubernetesObject.ApiVersion,
            kubernetesObject.Kind,
            kubernetesObject.Metadata.Name,
            kubernetesObject.Metadata.Uid);

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

    private static IList<V1OwnerReference> EnsureOwnerReferences(this V1ObjectMeta meta) =>
        meta.OwnerReferences ??= new List<V1OwnerReference>();
}
