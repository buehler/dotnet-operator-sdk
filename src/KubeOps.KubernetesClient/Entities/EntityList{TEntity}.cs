using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace KubeOps.KubernetesClient.Entities;

internal class EntityList<TEntity> : KubernetesObject
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ListMeta Metadata { get; set; } = new();

    [JsonPropertyName("items")]
    public IList<TEntity> Items { get; set; } = new List<TEntity>();
}
