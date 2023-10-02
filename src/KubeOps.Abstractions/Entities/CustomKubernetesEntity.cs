using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

/// <summary>
/// Base class for custom Kubernetes entities. The interface <see cref="IKubernetesObject{TMetadata}"/>
/// can be used on its own, but this class provides convenience initializers.
/// </summary>
public abstract class CustomKubernetesEntity : KubernetesObject, IKubernetesObject<V1ObjectMeta>
{
    /// <summary>
    /// The metadata of the kubernetes object.
    /// </summary>
    public V1ObjectMeta Metadata { get; set; } = new();
}
