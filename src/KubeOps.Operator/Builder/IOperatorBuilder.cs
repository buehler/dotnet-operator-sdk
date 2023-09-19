using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Builder;

public interface IOperatorBuilder
{
    IServiceCollection Services { get; }

    IOperatorBuilder AddEntityMetadata<TEntity>(EntityMetadata<TEntity> metadata)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    IOperatorBuilder AddController<TImplementation, TEntity>()
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    IOperatorBuilder AddController<TImplementation, TEntity>(EntityMetadata<TEntity> metadata)
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
