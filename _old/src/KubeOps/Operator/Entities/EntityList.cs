using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities;

/// <summary>
/// Type for a list of entities.
/// </summary>
/// <typeparam name="T">Type for the list entries.</typeparam>
public class EntityList<T> : KubernetesObject
    where T : IKubernetesObject<V1ObjectMeta>
{
    /// <summary>
    /// Official list metadata object of kubernetes.
    /// </summary>
    public V1ListMeta Metadata { get; set; } = new();

    /// <summary>
    /// The list of items.
    /// </summary>
    public IList<T> Items { get; set; } = new List<T>();
}
