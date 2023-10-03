using k8s;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Finalizer;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    public IOperatorBuilder AddFinalizer<TImplementation, TEntity>(string identifier)
        where TImplementation : class, IEntityFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.AddTransient<TImplementation>();
        Services.AddSingleton(new FinalizerRegistration(identifier, typeof(TImplementation), typeof(TEntity)));
        Services.AddTransient<EntityFinalizerAttacher<TImplementation, TEntity>>(services => async entity =>
        {
            var logger = services.GetService<ILogger<EntityFinalizerAttacher<TImplementation, TEntity>>>();
            var client = services.GetRequiredService<IKubernetesClient<TEntity>>();

            logger?.LogTrace(
                """Try to add finalizer "{finalizer}" on entity "{kind}/{name}".""",
                identifier,
                entity.Kind,
                entity.Name());

            if (!entity.AddFinalizer(identifier))
            {
                return entity;
            }

            logger?.LogInformation(
                """Added finalizer "{finalizer}" on entity "{kind}/{name}".""",
                identifier,
                entity.Kind,
                entity.Name());
            return await client.Update(entity);
        });

        return this;
    }
}
