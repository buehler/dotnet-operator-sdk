using System.Collections.Immutable;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Operator.Client;

internal class KubernetesClientFactory : IKubernetesClientFactory
{
    private readonly Dictionary<Type, EntityMetadata> _registeredMetadata = new();

    public IImmutableDictionary<Type, EntityMetadata> RegisteredMetadata => _registeredMetadata.ToImmutableDictionary();

    public void RegisterMetadata<TEntity>(EntityMetadata metadata)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _registeredMetadata[typeof(TEntity)] = metadata;
    }

    public GenericClient GetClient<TEntity>(IKubernetes? kubernetes = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        if (!_registeredMetadata.TryGetValue(typeof(TEntity), out var metadata))
        {
            throw new InvalidOperationException(
                $"No metadata registered for entity {typeof(TEntity).Name}. " +
                "Please register metadata for this entity before using the client.");
        }

        return metadata.Group switch
        {
            null => new GenericClient(
                kubernetes ?? new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig()),
                metadata.Version,
                metadata.PluralName),
            _ => new GenericClient(
                kubernetes ?? new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig()),
                metadata.Group,
                metadata.Version,
                metadata.PluralName),
        };
    }
}
