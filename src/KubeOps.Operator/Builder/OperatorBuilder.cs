using k8s;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Builder;

internal class OperatorBuilder : IOperatorBuilder
{
    public OperatorBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IOperatorBuilder AddEntity<TEntity>(EntityMetadata metadata)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.AddTransient<IKubernetesClient<TEntity>>(_ => new KubernetesClient<TEntity>(metadata));
        return this;
    }

    public IOperatorBuilder AddController<TImplementation, TEntity>()
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.AddScoped<IEntityController<TEntity>, TImplementation>();
        Services.AddHostedService<ResourceWatcher<TEntity>>();
        return this;
    }

    public IOperatorBuilder AddControllerWithEntity<TImplementation, TEntity>(EntityMetadata metadata)
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta> =>
        AddController<TImplementation, TEntity>().AddEntity<TEntity>(metadata);
}
