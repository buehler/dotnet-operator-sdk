using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Leadership;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Services;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Controller
{
    /// <summary>
    /// Resource controller base. Contains the logic to reconcile entities.
    /// This class must be overloaded for any entities one wants to manage.
    ///
    /// Overwrite the specific methods to use them for reconciliation.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public abstract class ResourceControllerBase<TEntity> : IResourceController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private static readonly IReadOnlyList<ResourceEventType> RequeueableEvents = new[]
        {
            ResourceEventType.Created,
            ResourceEventType.Updated,
            ResourceEventType.NotModified,
        };

        private readonly ILogger<ResourceControllerBase<TEntity>> _logger;
        private readonly IResourceServices<TEntity> _services;
        private bool _running;

        /// <summary>
        /// Construct the controller.
        /// </summary>
        /// <param name="services">The needed services for the controller to function properly.</param>
        protected ResourceControllerBase(IResourceServices<TEntity> services)
        {
            _logger = services.LoggerFactory.CreateLogger<ResourceControllerBase<TEntity>>();
            _services = services;
        }

        bool IResourceController.Running => _running;

        /// <summary>
        /// Instance of the <see cref="IKubernetesClient"/> for querying the cluster.
        /// </summary>
        protected IKubernetesClient Client => _services.Client;

        /// <summary>
        /// Start the controller. This does not necessarily mean the controller
        /// "runs" afterwards. Leader-election will take place first.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>A task for the async method.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(@"Startup CRD Controller for ""{resource}"".", typeof(TEntity));
            if (!string.IsNullOrWhiteSpace(_services.Settings.Namespace))
            {
                _logger.LogInformation(
                    @"The CRD controller for ""{resource}"" is namespaced to ""{namespace}"".",
                    typeof(TEntity),
                    _services.Settings.Namespace);
            }

            _running = true;
            _services.LeaderElection.LeadershipChange += LeadershipChanged;
            LeadershipChanged(null, _services.LeaderElection.State);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop the controller.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>A task for the async method.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(@"Shutdown CRD Controller for ""{resource}"".", typeof(TEntity));

            LeadershipChanged(null, LeaderState.None);
            _services.LeaderElection.LeadershipChange -= LeadershipChanged;
            _running = false;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called for <see cref="ResourceEventType.Created"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the created event.</param>
        /// <returns>
        /// A task with an optional <see cref="TimeSpan"/>. If null is returned,
        /// event will not be re-queued. If a <see cref="TimeSpan"/> is returned,
        /// the resource will be re-queued at the given point in time.
        /// </returns>
        protected virtual Task<TimeSpan?> Created(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""created"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        /// <summary>
        /// Called for <see cref="ResourceEventType.Updated"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the updated event.</param>
        /// <returns>
        /// A task with an optional <see cref="TimeSpan"/>. If null is returned,
        /// event will not be re-queued. If a <see cref="TimeSpan"/> is returned,
        /// the resource will be re-queued at the given point in time.
        /// </returns>
        protected virtual Task<TimeSpan?> Updated(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""updated"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        /// <summary>
        /// Called for <see cref="ResourceEventType.NotModified"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the not-modified event.</param>
        /// <returns>
        /// A task with an optional <see cref="TimeSpan"/>. If null is returned,
        /// event will not be re-queued. If a <see cref="TimeSpan"/> is returned,
        /// the resource will be re-queued at the given point in time.
        /// </returns>
        protected virtual Task<TimeSpan?> NotModified(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""not modified"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        /// <summary>
        /// Called for <see cref="ResourceEventType.StatusUpdated"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the status-modified event.</param>
        /// <returns>
        /// A task with an optional <see cref="TimeSpan"/>. If null is returned,
        /// event will not be re-queued. If a <see cref="TimeSpan"/> is returned,
        /// the resource will be re-queued at the given point in time.
        /// </returns>
        protected virtual Task StatusModified(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""status modified"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called for <see cref="ResourceEventType.Deleted"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the deleted event.</param>
        /// <returns>
        /// A task with an optional <see cref="TimeSpan"/>. If null is returned,
        /// event will not be re-queued. If a <see cref="TimeSpan"/> is returned,
        /// the resource will be re-queued at the given point in time.
        /// </returns>
        protected virtual Task Deleted(TEntity resource)
        {
            _logger.LogDebug(
                @"Object ""{kind}/{name}"" fired ""deleted"" event.",
                resource.Kind,
                resource.Metadata.Name);
            return Task.FromResult(default(TimeSpan?));
        }

        /// <summary>
        /// Attach a finalizer to a given resource.
        /// The finalizer is registered with it's name in the finalizer
        /// list of the resource.
        /// </summary>
        /// <param name="resource">The resource to attach the finalizer to.</param>
        /// <typeparam name="TFinalizer">The type of the finalizer.</typeparam>
        /// <returns>A task that resolves when the finalizer is added.</returns>
        /// <exception cref="NoFinalizerRegisteredException">When no finalizer for the given type is found.</exception>
        protected Task AttachFinalizer<TFinalizer>(TEntity resource)
            where TFinalizer : class, IResourceFinalizer<TEntity>
        {
            if (!(_services.Finalizers
                .Value
                .FirstOrDefault(f => f.GetType() == typeof(TFinalizer)) is TFinalizer finalizer))
            {
                throw new NoFinalizerRegisteredException(typeof(TEntity));
            }

            return finalizer.Register(resource);
        }

        private async void LeadershipChanged(object? sender, LeaderState state)
        {
            if (state == LeaderState.Leader)
            {
                _logger.LogInformation("This instance was elected as leader, starting event queue.");

                if (_services.Settings.PreloadCache)
                {
                    _logger.LogInformation("The 'preload cache' setting is set to 'true'.");
                    var items = await _services.Client.List<TEntity>(_services.Settings.Namespace);
                    _services.ResourceCache.Fill(items);
                }

                _services.EventQueue.ResourceEvent += OnResourceEvent;
                await _services.EventQueue.Start();

                return;
            }

            _logger.LogInformation(
                "This instance has either resigned from leadership, was elected candidate or is shutting down. Stopping event queue.");
            await _services.EventQueue.Stop();
            _services.EventQueue.ResourceEvent -= OnResourceEvent;
        }

        private async void OnResourceEvent(object? _, (ResourceEventType Type, TEntity Resource) args)
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

                    if (!(_services.Finalizers
                            .Value
                            .FirstOrDefault(
                                f => f.Identifier == resource.Metadata.Finalizers.First()) is
                        IResourceFinalizer<TEntity>
                        finalizer))
                    {
                        _logger.LogDebug(
                            @"Resource ""{kind}/{name}"" is in pending deletion but no suitable finalizer found.",
                            resource.Kind,
                            resource.Metadata.Name);
                        _services.EventQueue.ClearError(resource);
                        return;
                    }

                    _logger.LogInformation(
                        @"Resource ""{kind}/{name}"" is in pending deletion. Execute finalizer ""{finalizer}"".",
                        resource.Kind,
                        resource.Metadata.Name,
                        finalizer.Identifier);
                    await finalizer.FinalizeResource(resource);
                    _services.EventQueue.ClearError(resource);

                    return;
                }

                if (!RequeueableEvents.Contains(type))
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
                    _services.EventQueue.ClearError(resource);

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
                    await _services.EventQueue.Enqueue(resource, requeue);
                }
                else
                {
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not requested.",
                        type,
                        resource.Kind,
                        resource.Metadata.Name);
                }

                _services.EventQueue.ClearError(resource);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    @"Event type ""{eventType}"" on resource ""{kind}/{name}"" threw an error. Requeue with exponental backoff.",
                    type,
                    resource.Kind,
                    resource.Metadata.Name);
                _services.EventQueue.EnqueueErrored(type, resource);
            }
        }
    }
}
