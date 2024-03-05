using k8s;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Events;
using KubeOps.Abstractions.Finalizer;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Events;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.LeaderElection;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KubeOps.Operator.Builder;

internal class OperatorBuilder : IOperatorBuilder
{
    private readonly OperatorSettings _settings;

    public OperatorBuilder(IServiceCollection services, OperatorSettings settings)
    {
        _settings = settings;
        Services = services;
        AddOperatorBase();
    }

    public IServiceCollection Services { get; }

    public IOperatorBuilder AddController<TImplementation, TEntity>()
        where TImplementation : class, IEntityController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.AddHostedService<EntityRequeueBackgroundService<TEntity>>();
        Services.TryAddScoped<IEntityController<TEntity>, TImplementation>();
        Services.TryAddSingleton(new TimedEntityQueue<TEntity>());
        Services.TryAddTransient<IEntityRequeueFactory, KubeOpsEntityRequeueFactory>();
        Services.TryAddTransient<EntityRequeue<TEntity>>(services =>
            services.GetRequiredService<IEntityRequeueFactory>().Create<TEntity>());

        if (_settings.EnableLeaderElection)
        {
            Services.AddHostedService<LeaderAwareResourceWatcher<TEntity>>();
        }
        else
        {
            Services.AddHostedService<ResourceWatcher<TEntity>>();
        }

        return this;
    }

    public IOperatorBuilder AddFinalizer<TImplementation, TEntity>(string identifier)
        where TImplementation : class, IEntityFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.TryAddKeyedTransient<IEntityFinalizer<TEntity>, TImplementation>(identifier);
        Services.TryAddTransient<IEventFinalizerAttacherFactory, KubeOpsEventFinalizerAttacherFactory>();
        Services.TryAddTransient<EntityFinalizerAttacher<TImplementation, TEntity>>(services =>
            services.GetRequiredService<IEventFinalizerAttacherFactory>()
                .Create<TImplementation, TEntity>(identifier));

        return this;
    }

    private void AddOperatorBase()
    {
        Services.AddSingleton(_settings);

        // Add the default configuration and the client separately. This allows external users to override either
        // just the config (e.g. for integration tests) or to replace the whole client, e.g. with a mock.
        Services.TryAddSingleton(KubernetesClientConfiguration.BuildDefaultConfig());
        Services.TryAddTransient<IKubernetesClient>(services =>
            new KubernetesClient.KubernetesClient(services.GetRequiredService<KubernetesClientConfiguration>()));

        // We also add the k8s.IKubernetes as a transient service, in order to allow to access internal services
        // and also external users to make use of it's features that might not be implemented in the adapted client.
        Services.TryAddTransient<IKubernetes>(services =>
            new Kubernetes(services.GetRequiredService<KubernetesClientConfiguration>()));
        Services.TryAddTransient<IEventPublisherFactory, KubeOpsEventPublisherFactory>();
        Services.TryAddTransient<EventPublisher>(
            services => services.GetRequiredService<IEventPublisherFactory>().Create());

        if (_settings.EnableLeaderElection)
        {
            Services.AddLeaderElection();
        }
    }
}
