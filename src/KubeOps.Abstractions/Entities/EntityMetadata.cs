using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

public record EntityMetadata<TEntity>(string Kind, string Version, string? Group = null, string? Plural = null)
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    public readonly Type EntityType = typeof(TEntity);

    public string PluralName => Plural ?? $"{Kind.ToLower()}s";
}
