using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Kubernetes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static KubeOps.Operator.Builder.IComponentRegistrar;

namespace KubeOps.Operator.Controller
{
    internal class ManagedResourceController<TEntity> : IManagedResourceController
        where TEntity : class, IKubernetesObject<V1ObjectMeta>
    {
        private const byte MaxRetries = 4;

        private readonly Random _rnd = new();

        private readonly ILogger<ManagedResourceController<TEntity>> _logger;
        private readonly IKubernetesClient _client;
        private readonly ResourceWatcher<TEntity> _watcher;
        private readonly ResourceCache<TEntity> _cache;
        private readonly IServiceProvider _services;
        private readonly ResourceControllerMetrics<TEntity> _metrics;
        private readonly OperatorSettings _settings;
        private readonly ControllerRegistration _controllerRegistration;

        private readonly Subject<RequeuedEvent>
            _requeuedEvents = new();

        private readonly Subject<QueuedEvent>
            _erroredEvents = new();

        private IDisposable? _eventSubscription;

        public ManagedResourceController(
            ILogger<ManagedResourceController<TEntity>> logger,
            IKubernetesClient client,
            ResourceWatcher<TEntity> watcher,
            ResourceCache<TEntity> cache,
            IServiceProvider services,
            ResourceControllerMetrics<TEntity> metrics,
            OperatorSettings settings,
            ControllerRegistration controllerRegistration)
        {
            _logger = logger;
            _client = client;
            _watcher = watcher;
            _cache = cache;
            _services = services;
            _metrics = metrics;
            _settings = settings;
            _controllerRegistration = controllerRegistration;
        }

        private IObservable<Unit> WatcherEvents => _watcher
            .WatchEvents
            .Select(MapWatchEvent)
            .Do(
                data => _logger.LogTrace(
                    @"Mapped watch event to ""{resourceEventType}"" for ""{kind}/{name}""",
                    data.ResourceEvent,
                    data.Resource.Kind,
                    data.Resource.Name()))
            .Where(data => data.ResourceEvent != ResourceEventType.FinalizerModified)
            .Do(_ => _metrics.EventsFromWatcher.Inc())
            .Select(
                data => Observable.FromAsync(
                    () => data.ResourceEvent == ResourceEventType.Finalizing
                        ? HandleResourceFinalization(data)
                        : HandleResourceEvent(data),
                    ThreadPoolScheduler.Instance))
            .Merge();

        private IObservable<Unit> RequeuedEvents => _requeuedEvents
            .Do(_ => _metrics.RequeuedEvents.Inc())
            .Select(
                data => Observable.Return(data).Delay(data.Delay))
            .Switch()
            .Select(data =>
                Observable.FromAsync(async () =>
                {
                    var queuedEvent = await UpdateResourceData(data.Resource);

                    return data.ResourceEvent.HasValue && queuedEvent != null
                        ? queuedEvent with { ResourceEvent = data.ResourceEvent.Value }
                        : queuedEvent;
                }))
            .Switch()
            .Where(data => data != null)
            .Do(
                data => _logger.LogTrace(
                    @"Mapped requeued resource event to ""{resourceEventType}"" for ""{kind}/{name}""",
                    data?.ResourceEvent,
                    data?.Resource.Kind,
                    data?.Resource.Name()))
            .Where(
                data =>
                    data?.ResourceEvent != ResourceEventType.Finalizing &&
                    data?.ResourceEvent != ResourceEventType.FinalizerModified)
            .Select(
                data => Observable.FromAsync(
                    () => HandleResourceEvent(data), // this default is never gonna happen.
                    ThreadPoolScheduler.Instance))
            .Merge();

        private IObservable<Unit> ErroredEvents => _erroredEvents
            .Do(_ => _metrics.ErroredEvents.Inc())
            .Select(
                data =>
                {
                    var (resourceEventType, resource, retryCount) = data;
                    if (retryCount <= MaxRetries)
                    {
                        var backoff = ExponentialBackoff(retryCount);
                        _logger.LogDebug(
                            @"Retry attempt {retryCount} for event ""{eventType}"" on resource ""{kind}/{name}"" with exponential backoff ""{backoff}"".",
                            retryCount,
                            resourceEventType,
                            resource.Kind,
                            resource.Name(),
                            backoff);
                        return Observable.Return(data).Delay(backoff);
                    }

                    _logger.LogError(
                        @"Event ""{eventType}"" on resource ""{kind}/{name}"" threw too many errors. Skipping Event.",
                        resourceEventType,
                        resource.Kind,
                        resource.Name());
                    return Observable.Return<QueuedEvent?>(null);
                })
            .Switch()
            .Select(
                data => Observable.FromAsync(
                    () => data?.ResourceEvent == ResourceEventType.Finalizing
                        ? HandleResourceFinalization(data)
                        : HandleResourceEvent(data),
                    ThreadPoolScheduler.Instance))
            .Merge();

        public virtual async Task StartAsync()
        {
            if (_settings.PreloadCache)
            {
                _logger.LogInformation("The 'preload cache' setting is set to 'true'.");
                var items = await _client.List<TEntity>(_settings.Namespace);
                _cache.Fill(items);
            }

            _logger.LogDebug(@"Managed resource controller startup for type ""{type}"".", typeof(TEntity));
            _eventSubscription = WatcherEvents
                .Merge(RequeuedEvents)
                .Merge(ErroredEvents)
                .Subscribe();

            await _watcher.Start();
            _metrics.Running.Set(1);
        }

        public virtual async Task StopAsync()
        {
            _logger.LogTrace(@"Managed resource controller shutdown for type ""{type}"".", typeof(TEntity));
            await _watcher.Stop();
            _eventSubscription?.Dispose();
            _eventSubscription = null;
            _cache.Clear();
            _metrics.Running.Set(0);
        }

        public void Dispose()
        {
            _logger.LogTrace(@"Managed resource controller disposal for type ""{type}"".", typeof(TEntity));
            _watcher.Dispose();
            _eventSubscription?.Dispose();
            _eventSubscription = null;
            _metrics.Running.Set(0);
        }

        protected async Task HandleResourceEvent(QueuedEvent? data)
        {
            if (data == null)
            {
                return;
            }

            var (@event, resource, _) = data;
            _logger.LogDebug(
                @"Execute/Reconcile event ""{eventType}"" on resource ""{kind}/{name}"".",
                @event,
                resource.Kind,
                resource.Name());

            var controllerType = _controllerRegistration.ControllerType;

            ResourceControllerResult? result = null;
            _logger.LogTrace(@"Instantiating new DI scope for controller ""{name}"".", controllerType.Name);
            using (var scope = _services.CreateScope())
            {
                if (!(scope.ServiceProvider.GetRequiredService(controllerType) is IResourceController<TEntity>
                    controller))
                {
                    var ex = new InvalidCastException(
                        $@"The type ""{controllerType.Namespace}.{controllerType.Name}"" is not a valid IResourceController<TEntity> type.");
                    _logger.LogCritical(
                        @"The type ""{namespace}.{name}"" is not a valid IResourceController<TEntity> type.",
                        controllerType.Namespace,
                        controllerType.Name);
                    throw ex;
                }

                try
                {
                    switch (@event)
                    {
                        case ResourceEventType.Created:
                            result = await controller.CreatedAsync(resource);
                            _metrics.CreatedEvents.Inc();
                            break;
                        case ResourceEventType.Updated:
                            result = await controller.UpdatedAsync(resource);
                            _metrics.UpdatedEvents.Inc();
                            break;
                        case ResourceEventType.NotModified:
                            result = await controller.NotModifiedAsync(resource);
                            _metrics.NotModifiedEvents.Inc();
                            break;
                        case ResourceEventType.Deleted:
                            await controller.DeletedAsync(resource);
                            _metrics.DeletedEvents.Inc();
                            _logger.LogInformation(
                                @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not possible.",
                                @event,
                                resource.Kind,
                                resource.Name());
                            return;
                        case ResourceEventType.StatusUpdated:
                            await controller.StatusModifiedAsync(resource);
                            _metrics.StatusUpdatedEvents.Inc();
                            _logger.LogInformation(
                                @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not possible.",
                                @event,
                                resource.Kind,
                                resource.Name());
                            return;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" threw an error. Retry attempt {retryAttempt}.",
                        @event,
                        resource.Kind,
                        resource.Name(),
                        data.RetryCount + 1);
                    _erroredEvents.OnNext(data with { RetryCount = data.RetryCount + 1 });
                    return;
                }
            }

            switch (result)
            {
                case null:
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not requested.",
                        @event,
                        resource.Kind,
                        resource.Name());
                    return;
                case RequeueEventResult requeue:
                    if (_settings.DefaultRequeueAsSameType)
                    {
                        requeue = new RequeueEventResult(requeue.RequeueIn, @event);
                    }

                    if (requeue.EventType.HasValue)
                    {
                        _logger.LogInformation(
                            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue requested as type ""{requeueType}"" with delay ""{requeue}"".",
                            @event,
                            resource.Kind,
                            resource.Name(),
                            requeue.EventType,
                            requeue.RequeueIn);
                    }
                    else
                    {
                        _logger.LogInformation(
                            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue requested with delay ""{requeue}"".",
                            @event,
                            resource.Kind,
                            resource.Name(),
                            requeue.RequeueIn);
                    }

                    _requeuedEvents.OnNext(new RequeuedEvent(requeue.EventType, resource, requeue.RequeueIn));
                    break;
            }
        }

        protected async Task HandleResourceFinalization(QueuedEvent? data)
        {
            using var scope = _services.CreateScope();

            if (data == null)
            {
                return;
            }

            var (_, resource, retryCount) = data;

            _logger.LogDebug(
                @"Finalize resource ""{kind}/{name}"".",
                resource.Kind,
                resource.Name());

            try
            {
                await scope.ServiceProvider.GetRequiredService<IFinalizerManager<TEntity>>()
                    .FinalizeAsync(data.Resource);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    @"Finalize resource ""{kind}/{name}"" threw an error. Retry attempt {retryAttempt}.",
                    resource.Kind,
                    resource.Name(),
                    retryCount + 1);
                _erroredEvents.OnNext(data with { RetryCount = retryCount + 1 });
                return;
            }
        }

        private (ResourceEventType ResourceEvent, TEntity Resource) MapCacheResult(
            CacheComparisonResult state,
            TEntity resource)
        {
            _logger.LogTrace(
                @"Mapping cache result ""{cacheResult}"" for ""{kind}/{name}"".",
                state,
                resource.Kind,
                resource.Name());

            switch (state)
            {
                case CacheComparisonResult.New when resource.Metadata.DeletionTimestamp != null:
                case CacheComparisonResult.Modified when resource.Metadata.DeletionTimestamp != null:
                case CacheComparisonResult.NotModified when resource.Metadata.DeletionTimestamp != null:
                    return (ResourceEventType.Finalizing, resource);
                case CacheComparisonResult.New:
                    return (ResourceEventType.Created, resource);
                case CacheComparisonResult.Modified:
                    return (ResourceEventType.Updated, resource);
                case CacheComparisonResult.StatusModified:
                    return (ResourceEventType.StatusUpdated, resource);
                case CacheComparisonResult.NotModified:
                    return (ResourceEventType.NotModified, resource);
                case CacheComparisonResult.FinalizersModified:
                    return (ResourceEventType.FinalizerModified, resource);
                default:
                    var ex = new ArgumentException("The caching state is out of the processable range", nameof(state));
                    _logger.LogCritical(
                        ex,
                        @"The caching state ""{cacheState}"" is not further processable for controller handling for the resource ""{kind}/{name}"".",
                        state,
                        resource.Kind,
                        resource.Name());
                    throw ex;
            }
        }

        private QueuedEvent MapWatchEvent(
            (WatchEventType Event, TEntity Resource) data)
        {
            var (watchEventType, resource) = data;

            _logger.LogTrace(
                @"Mapping watcher event ""{watchEvent}"" for ""{kind}/{name}"".",
                watchEventType,
                resource.Kind,
                resource.Name());

            switch (watchEventType)
            {
                case WatchEventType.Added:
                case WatchEventType.Modified:
                    resource = _cache.Upsert(resource, out var state);
                    var (@event, cachedResource) = MapCacheResult(state, resource);
                    return new QueuedEvent(@event, cachedResource);
                case WatchEventType.Deleted:
                    _cache.Remove(resource);
                    return new QueuedEvent(ResourceEventType.Deleted, resource);
            }

            var ex = new ArgumentException(
                "The watcher event is not processable (only added / modified / deleted allowed).",
                nameof(data));
            _logger.LogCritical(
                ex,
                @"The watcher event ""{watchEvent}"" is not further processable for the resource ""{kind}/{name}"".",
                watchEventType,
                resource.Kind,
                resource.Name());

            throw ex;
        }

        private async Task<QueuedEvent?> UpdateResourceData(
            TEntity resource)
        {
            _logger.LogTrace(
                @"Update resource from k8s / cache for delayed requeue for ""{kind}/{name}"".",
                resource.Kind,
                resource.Name());

            var newResource = await _client.Get<TEntity>(
                resource.Name(),
                resource.Namespace());

            if (newResource == null)
            {
                _cache.Remove(resource);
                _logger.LogDebug(
                    @"Resource ""{kind}/{name}"" for enqueued event was not present anymore.",
                    resource.Kind,
                    resource.Name());
                return null;
            }

            newResource = _cache.Upsert(newResource, out var state);
            var (@event, cachedResource) = MapCacheResult(state, newResource);
            return new QueuedEvent(@event, cachedResource);
        }

        private TimeSpan ExponentialBackoff(int retryCount) => TimeSpan
            .FromSeconds(Math.Pow(2, retryCount))
            .Add(TimeSpan.FromMilliseconds(_rnd.Next(0, 1000)));

        internal record QueuedEvent(ResourceEventType ResourceEvent, TEntity Resource, int RetryCount = 0);

        private record RequeuedEvent(ResourceEventType? ResourceEvent, TEntity Resource, TimeSpan Delay);
    }
}
