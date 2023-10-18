using System.Security.Cryptography;
using System.Text;

using k8s;
using k8s.LeaderElection;
using k8s.LeaderElection.ResourceLock;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Events;
using KubeOps.Abstractions.Finalizer;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        Services.AddScoped<IEntityController<TEntity>, TImplementation>();
        Services.AddSingleton(new TimedEntityQueue<TEntity>());
        Services.AddTransient(CreateEntityRequeue<TEntity>());

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
        Services.AddTransient<TImplementation>();
        Services.AddSingleton(new FinalizerRegistration(identifier, typeof(TImplementation), typeof(TEntity)));
        Services.AddTransient(CreateFinalizerAttacher<TImplementation, TEntity>(identifier));

        return this;
    }

    private static Func<IServiceProvider, EntityFinalizerAttacher<TImplementation, TEntity>> CreateFinalizerAttacher<
        TImplementation, TEntity>(
        string identifier)
        where TImplementation : class, IEntityFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => services => async entity =>
        {
            var logger = services.GetService<ILogger<EntityFinalizerAttacher<TImplementation, TEntity>>>();
            using var client = new KubernetesClient.KubernetesClient();

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
            return await client.UpdateAsync(entity);
        };

    private static Func<IServiceProvider, EntityRequeue<TEntity>> CreateEntityRequeue<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => services => (entity, timeSpan) =>
        {
            var logger = services.GetService<ILogger<EntityRequeue<TEntity>>>();
            var queue = services.GetRequiredService<TimedEntityQueue<TEntity>>();

            logger?.LogTrace(
                """Requeue entity "{kind}/{name}" in {milliseconds}ms.""",
                entity.Kind,
                entity.Name(),
                timeSpan.TotalMilliseconds);

            queue.Enqueue(entity, timeSpan);
        };

    private static Func<IServiceProvider, EventPublisher> CreateEventPublisher()
        => services =>
            async (entity, reason, message, type) =>
            {
                var logger = services.GetService<ILogger<EventPublisher>>();
                using var client = new KubernetesClient.KubernetesClient() as IKubernetesClient;
                var settings = services.GetRequiredService<OperatorSettings>();

                var @namespace = entity.Namespace() ?? "default";
                logger?.LogTrace(
                    "Encoding event name with: {resourceName}.{resourceNamespace}.{reason}.{message}.{type}.",
                    entity.Name(),
                    @namespace,
                    reason,
                    message,
                    type);

                var eventName = $"{entity.Name()}.{@namespace}.{reason}.{message}.{type}";
                var encodedEventName =
                    Convert.ToHexString(
                        SHA512.HashData(
                            Encoding.UTF8.GetBytes(eventName)));

                logger?.LogTrace("""Search or create event with name "{name}".""", encodedEventName);

                var @event = await client.GetAsync<Corev1Event>(encodedEventName, @namespace) ??
                             new Corev1Event
                             {
                                 Metadata = new()
                                 {
                                     Name = encodedEventName,
                                     NamespaceProperty = @namespace,
                                     Annotations =
                                         new Dictionary<string, string>
                                         {
                                             { "originalName", eventName },
                                             { "nameHash", "sha512" },
                                             { "nameEncoding", "Hex String" },
                                         },
                                 },
                                 Type = type.ToString(),
                                 Reason = reason,
                                 Message = message,
                                 ReportingComponent = settings.Name,
                                 ReportingInstance = Environment.MachineName,
                                 Source = new() { Component = settings.Name, },
                                 InvolvedObject = entity.MakeObjectReference(),
                                 FirstTimestamp = DateTime.UtcNow,
                                 LastTimestamp = DateTime.UtcNow,
                                 Count = 0,
                             }.Initialize();

                @event.Count++;
                @event.LastTimestamp = DateTime.UtcNow;
                logger?.LogTrace(
                    "Save event with new count {count} and last timestamp {timestamp}",
                    @event.Count,
                    @event.LastTimestamp);

                try
                {
                    await client.SaveAsync(@event);
                    logger?.LogInformation(
                        """Created or updated event with name "{name}" to new count {count} on entity "{kind}/{name}".""",
                        eventName,
                        @event.Count,
                        entity.Kind,
                        entity.Name());
                }
                catch (Exception e)
                {
                    logger?.LogError(
                        e,
                        """Could not publish event with name "{name}" on entity "{kind}/{name}".""",
                        eventName,
                        entity.Kind,
                        entity.Name());
                }
            };

    private void AddOperatorBase()
    {
        Services.AddSingleton(_settings);
        Services.AddTransient<IKubernetesClient>(_ => new KubernetesClient.KubernetesClient());
        Services.AddTransient(CreateEventPublisher());

        if (_settings.EnableLeaderElection)
        {
            using var client = new KubernetesClient.KubernetesClient();

            var elector = new LeaderElector(
                new LeaderElectionConfig(
                    new LeaseLock(
                        new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig()),
                        client.GetCurrentNamespace(),
                        $"{_settings.Name}-leader",
                        Environment.MachineName))
                {
                    LeaseDuration = _settings.LeaderElectionLeaseDuration,
                    RenewDeadline = _settings.LeaderElectionRenewDeadline,
                    RetryPeriod = _settings.LeaderElectionRetryPeriod,
                });
            Services.AddSingleton(elector);
            elector.RunAsync();
        }
    }
}
