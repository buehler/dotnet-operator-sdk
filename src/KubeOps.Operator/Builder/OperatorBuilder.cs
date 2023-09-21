using k8s;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Operator.Client;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Builder;

internal class OperatorBuilder : IOperatorBuilder
{
    private readonly IKubernetesClientFactory _entityClientFactory = new KubernetesClientFactory();

    public OperatorBuilder(IServiceCollection services)
    {
        Services = services;
        Services.AddSingleton(_entityClientFactory);
    }

    public IServiceCollection Services { get; }

    public IOperatorBuilder AddEntityMetadata<TEntity>(EntityMetadata metadata)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _entityClientFactory.RegisterMetadata<TEntity>(metadata);
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

    public IOperatorBuilder AddController<TImplementation, TEntity>(EntityMetadata metadata)
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta> =>
        AddController<TImplementation, TEntity>().AddEntityMetadata<TEntity>(metadata);
}
