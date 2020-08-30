using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Client;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Watcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Controller
{

    public class ResourceServices<TEntity> where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        public ResourceServices(ILoggerFactory loggerFactory,
            IKubernetesClient client,
            IResourceCache<TEntity> resourceCache,
            IResourceEventQueue<TEntity> eventQueue,
            Lazy<IEnumerable<IResourceFinalizer<TEntity>>> finalizers,
            OperatorSettings settings)
        {
            LoggerFactory = loggerFactory;
            Client = client;
            ResourceCache = resourceCache;
            EventQueue = eventQueue;
            Finalizers = finalizers;
            Settings = settings;
        }

        public ILoggerFactory LoggerFactory { get; }
        public IKubernetesClient Client { get; }
        public IResourceCache<TEntity> ResourceCache { get; }
        public IResourceEventQueue<TEntity> EventQueue { get; }
        public Lazy<IResourceWatcher<TEntity>> ResourceWatcher { get; }
        public Lazy<IEnumerable<IResourceFinalizer<TEntity>>> Finalizers { get; }
        public OperatorSettings Settings { get; }
    }

    public class LazyService<T> : Lazy<T> where T : class
    {
        public LazyService(IServiceProvider provider)
            : base(() => provider.GetRequiredService<T>())
        {
        }
    }

    // TODO: namespaced controller (only watch resource of a specific namespace)
    // TODO: Webhooks?

    public abstract class ResourceControllerBase<TEntity> : IResourceController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly IReadOnlyList<ResourceEventType> _requeueableEvents = new[]
        {
            ResourceEventType.Created,
            ResourceEventType.Updated,
            ResourceEventType.NotModified,
        };

        private readonly ILogger<ResourceControllerBase<TEntity>> _logger;
        private readonly IResourceEventQueue<TEntity> _eventQueue;
        private readonly Lazy<IEnumerable<IResourceFinalizer<TEntity>>> _finalizers;
        private bool _running;

        protected ResourceControllerBase(ResourceServices<TEntity> services)
        {
            _logger = services.LoggerFactory.CreateLogger< ResourceControllerBase<TEntity>>();
            _eventQueue = services.EventQueue;
            _finalizers = services.Finalizers;
            Services = services;
        }

        bool IResourceController.Running => _running;

        protected IKubernetesClient Client => Services.Client;
        protected ResourceServices<TEntity> Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(@"Startup CRD Controller for ""{resource}"".", typeof(TEntity));

            _eventQueue.ResourceEvent += OnResourceEvent;
            await _eventQueue.Start();
            _running = true;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(@"Shutdown CRD Controller for ""{resource}"".", typeof(TEntity));

            _eventQueue.Stop();
            _eventQueue.ResourceEvent -= OnResourceEvent;
            _running = false;
            return Task.CompletedTask;
        }

        protected virtual Task<TimeSpan?> Created(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""created"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        protected virtual Task<TimeSpan?> Updated(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""updated"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        protected virtual Task<TimeSpan?> NotModified(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""not modified"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        protected virtual Task StatusModified(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""status modified"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.CompletedTask;
        }

        protected virtual Task Deleted(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""deleted"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        private async void OnResourceEvent(object? _, (ResourceEventType type, TEntity resource) args)
        {
            var (type, resource) = args;

            try
            {
                _logger.LogDebug(
                    @"Execute event ""{eventType}"" on resource ""{kind}/{name}"".",
                    type,
                    resource.Kind,
                    resource.Metadata.Name);

                if (type == ResourceEventType.Finalizing)
                {
                    if (resource.Metadata.Finalizers == null || resource.Metadata.Finalizers.Count == 0)
                    {
                        return;
                    }

                    var finalizer = _finalizers.Value.FirstOrDefault(f => f.Identifier == resource.Metadata.Finalizers.First());
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
                        case ResourceEventType.StatusUpdated:
                            await StatusModified(resource);
                            break;
                        case ResourceEventType.Deleted:
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
                    ResourceEventType.Created => await Created(resource),
                    ResourceEventType.Updated => await Updated(resource),
                    ResourceEventType.NotModified => await NotModified(resource),
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
