using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Builder;

internal class OperatorBuilder : IOperatorBuilder
{
    public OperatorBuilder(IServiceCollection services)
    {
        Services = services;
        // Services.AddSingleton(new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig()));
    }

    public IServiceCollection Services { get; }

    public IOperatorBuilder AddEntityMetadata<TEntity>(EntityMetadata<TEntity> metadata) where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        throw new NotImplementedException();
    }

    public IOperatorBuilder AddController<TImplementation, TEntity>()
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.AddScoped<TImplementation>();
        Services.AddHostedService<ResourceWatcher<TEntity>>();
        return this;
    }

    public IOperatorBuilder AddController<TImplementation, TEntity>(EntityMetadata<TEntity> metadata)
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.AddScoped<TImplementation>();
        Services.AddHostedService<ResourceWatcher<TEntity>>();
        Services.AddSingleton(metadata);
        return this;
    }
}
