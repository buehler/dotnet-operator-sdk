using System.Security.Cryptography;
using System.Text;

using k8s;
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
    public OperatorBuilder(IServiceCollection services)
    {
        Services = services;
        Services.AddTransient<IKubernetesClient<Corev1Event>>(_ => new KubernetesClient<Corev1Event>(new(
            Corev1Event.KubeKind, Corev1Event.KubeApiVersion, Plural: Corev1Event.KubePluralName)));
        Services.AddTransient<EventPublisher>(
            services =>
                async (entity, reason, message, type) =>
                {
                    var logger = services.GetService<ILogger<EventPublisher>>();
                    var client = services.GetRequiredService<IKubernetesClient<Corev1Event>>();

                    var @namespace = entity.Namespace() ?? "default";
                    logger?.LogTrace(
                        "Encoding event name with: {resourceName}.{resourceNamespace}.{reason}.{message}.{type}.",
                        entity.Name(),
                        @namespace,
                        reason,
                        message,
                        type);

                    var eventName =
                        Convert.ToHexString(
                            SHA512.HashData(
                                Encoding.UTF8.GetBytes(
                                    $"{entity.Name()}.{@namespace}.{reason}.{message}.{type}")));

                    logger?.LogTrace("""Search or create event with name "{name}".""", eventName);

                    var @event = await client.GetAsync(eventName, @namespace) ??
                                 new Corev1Event
                                 {
                                     Kind = Corev1Event.KubeKind,
                                     ApiVersion = Corev1Event.KubeApiVersion,
                                     Metadata = new()
                                     {
                                         Name = eventName,
                                         NamespaceProperty = @namespace,
                                         Annotations =
                                             new Dictionary<string, string>
                                             {
                                                 {
                                                     "originalName",
                                                     $"{entity.Name()}.{@namespace}.{reason}.{message}.{type}"
                                                 },
                                                 { "nameHash", "sha512" },
                                                 { "nameEncoding", "Hex String" },
                                             },
                                     },
                                     Type = type.ToString(),
                                     Reason = reason,
                                     Message = message,
                                     // ReportingComponent = _settings.Name,
                                     ReportingInstance = Environment.MachineName,
                                     // Source = new() { Component = _settings.Name, },
                                     InvolvedObject = entity.MakeObjectReference(),
                                     FirstTimestamp = DateTime.UtcNow,
                                     LastTimestamp = DateTime.UtcNow,
                                     Count = 0,
                                 };

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
                });
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
        Services.AddSingleton(new TimedEntityQueue<TEntity>());
        Services.AddTransient<EntityRequeue<TEntity>>(services => (entity, timespan) =>
        {
            var logger = services.GetService<ILogger<EntityRequeue<TEntity>>>();
            var queue = services.GetRequiredService<TimedEntityQueue<TEntity>>();

            logger?.LogTrace(
                """Requeue entity "{kind}/{name}" in {milliseconds}ms.""",
                entity.Kind,
                entity.Name(),
                timespan.TotalMilliseconds);

            queue.Enqueue(entity, timespan);
        });

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
            return await client.UpdateAsync(entity);
        });

        return this;
    }
}
