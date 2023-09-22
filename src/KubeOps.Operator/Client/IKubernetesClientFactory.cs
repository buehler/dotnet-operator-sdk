using System.Collections.Immutable;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Operator.Client;

public interface IKubernetesClientFactory
{
    IImmutableDictionary<Type, EntityMetadata> RegisteredMetadata { get; }

    void RegisterMetadata<TEntity>(EntityMetadata metadata)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    GenericClient GetClient<TEntity>(IKubernetes? kubernetes = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
