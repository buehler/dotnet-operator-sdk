using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Client;
using KubeOps.Operator.DependencyInjection;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Controller
{
    // TODO: namespaced controller (only watch resource of a specific namespace)
    // TODO: Webhooks?

    public abstract class ResourceControllerBase<TResource> : IHostedService
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private readonly IReadOnlyList<EntityEventType> _requeueableEvents = new[]
        {
            EntityEventType.Created,
            EntityEventType.Updated,
            EntityEventType.NotModified,
        };

        private readonly ILogger<ResourceControllerBase<TResource>> _logger;
        private readonly EntityEventQueue<TResource> _eventQueue;

        private readonly Lazy<IKubernetesClient> _client =
            new Lazy<IKubernetesClient>(() => DependencyInjector.Services.GetRequiredService<IKubernetesClient>());

        protected ResourceControllerBase()
        {
            _logger = DependencyInjector.Services.GetRequiredService<ILogger<ResourceControllerBase<TResource>>>();
            _eventQueue = new EntityEventQueue<TResource>();
        }

        protected IKubernetesClient Client => _client.Value;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(@"Startup CRD Controller for ""{resource}"".", typeof(TResource));

            _eventQueue.ResourceEvent += OnResourceEvent;
            await _eventQueue.Start();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(@"Shutdown CRD Controller for ""{resource}"".", typeof(TResource));

            _eventQueue.Stop();
            _eventQueue.ResourceEvent -= OnResourceEvent;

            return Task.CompletedTask;
        }

        protected virtual Task<TimeSpan?> Created(TResource resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""created"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        protected virtual Task<TimeSpan?> Updated(TResource resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""updated"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        protected virtual Task<TimeSpan?> NotModified(TResource resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""not modified"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        protected virtual Task StatusModified(TResource resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""status modified"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.CompletedTask;
        }

        protected virtual Task Deleted(TResource resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""deleted"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        private async void OnResourceEvent(object? _, (EntityEventType type, TResource resource) args)
        {
            var (type, resource) = args;

            try
            {
                _logger.LogDebug(
                    @"Execute event ""{eventType}"" on resource ""{kind}/{name}"".",
                    type,
                    resource.Kind,
                    resource.Metadata.Name);

                if (type == EntityEventType.Finalizing)
                {
                    if (resource.Metadata.Finalizers == null || resource.Metadata.Finalizers.Count == 0)
                    {
                        return;
                    }

                    var finalizer = DependencyInjector.Services
                        .GetServices<IResourceFinalizer<TResource>>()
                        .FirstOrDefault(f => f.Identifier == resource.Metadata.Finalizers.First());
                    if (finalizer == null)
                    {
                        _logger.LogDebug(
                            @"Resource ""{kind}/{name}"" is in pending deletion but no suitable finalizer found.",
                            resource.Kind,
                            resource.Metadata.Name);
                        _eventQueue.ClearError(resource);
                        return;
                    }

                    _logger.LogInformation(
                        @"Resource ""{kind}/{name}"" is in pending deletion. Execute finalizer ""{finalizer}"".",
                        resource.Kind,
                        resource.Metadata.Name,
                        finalizer.Identifier);
                    await finalizer.FinalizeResource(resource);
                    _eventQueue.ClearError(resource);

                    return;
                }

                if (!_requeueableEvents.Contains(type))
                {
                    switch (type)
                    {
                        case EntityEventType.StatusUpdated:
                            await StatusModified(resource);
                            break;
                        case EntityEventType.Deleted:
                            await Deleted(resource);
                            break;
                    }

                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not possible.",
                        type,
                        resource.Kind,
                        resource.Metadata.Name);
                    _eventQueue.ClearError(resource);

                    return;
                }

                var requeue = type switch
                {
                    EntityEventType.Created => await Created(resource),
                    EntityEventType.Updated => await Updated(resource),
                    EntityEventType.NotModified => await NotModified(resource),
                    _ => throw new ArgumentOutOfRangeException(),
                };

                if (requeue != null)
                {
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue requested with delay ""{requeue}"".",
                        type,
                        resource.Kind,
                        resource.Metadata.Name,
                        requeue);
                    await _eventQueue.Enqueue(resource, requeue);
                }
                else
                {
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not requested.",
                        type,
                        resource.Kind,
                        resource.Metadata.Name);
                }

                _eventQueue.ClearError(resource);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    @"Event type ""{eventType}"" on resource ""{kind}/{name}"" threw an error. Requeue with exponental backoff.",
                    type,
                    resource.Kind,
                    resource.Metadata.Name);
                _eventQueue.EnqueueErrored(type, resource);
            }
        }
    }
}
