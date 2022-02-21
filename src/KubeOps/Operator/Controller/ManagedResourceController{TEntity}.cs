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

namespace KubeOps.Operator.Controller;

internal class ManagedResourceController<TEntity> : IManagedResourceController
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    private readonly ILogger<ManagedResourceController<TEntity>> _logger;
    private readonly IKubernetesClient _client;
    private readonly ResourceWatcher<TEntity> _watcher;
    private readonly ResourceCache<TEntity> _cache;
    private readonly IServiceProvider _services;
    private readonly ResourceControllerMetrics<TEntity> _metrics;
    private readonly OperatorSettings _settings;
    private readonly ControllerRegistration _controllerRegistration;

    private readonly Subject<ResourceEvent> _localEvents = new();

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

    private IObservable<Unit> Events => _localEvents
        .Where(EventRetryCountIsLessThanMax)
        .Merge(WatcherEvents)
        .GroupBy(e => e.Resource.Uid())
        .Select(
            group => group
                .Select(EventDelay)
                .Switch())
        .Merge()
        .Select(UpdateResourceData)
        .Merge()
        .Where(EventTypeIsNotFinalizerModified)
        .Select(HandleEvent)
        .Concat();

    private IObservable<ResourceEvent> WatcherEvents => _watcher
        .WatchEvents
        .Select(MapToResourceEvent)
        .Do(_ => _metrics.EventsFromWatcher.Inc());

    private static IObservable<ResourceEvent> EventDelay(ResourceEvent resourceEvent)
    {
        var delay = resourceEvent.Delay ?? TimeSpan.Zero;
        return Observable.Return(resourceEvent).DelaySubscription(delay);
    }

    private static bool EventTypeIsNotFinalizerModified(ResourceEvent resourceEvent) =>
        resourceEvent.Type != ResourceEventType.FinalizerModified;

    private bool EventRetryCountIsLessThanMax(ResourceEvent resourceEvent)
    {
        (ResourceEventType type, TEntity resource, var attempt, TimeSpan? delay) = resourceEvent;

        if (attempt == 0)
        {
            return true;
        }

        if (attempt <= _settings.MaxErrorRetries)
        {
            _logger.LogDebug(
                @"Retry attempt {retryCount} for event ""{eventType}"" on resource ""{kind}/{name}"" with exponential backoff ""{backoff}"".",
                attempt,
                type,
                resource.Kind,
                resource.Name(),
                delay);
            return true;
        }

        _logger.LogError(
            @"Event ""{eventType}"" on resource ""{kind}/{name}"" threw too many errors. Skipping Event.",
            resourceEvent,
            resource.Kind,
            resource.Name());

        return false;
    }

    public virtual async Task StartAsync()
    {
        if (_settings.PreloadCache)
        {
            _logger.LogInformation("The 'preload cache' setting is set to 'true'.");
            var items = await _client.List<TEntity>(_settings.Namespace);
            _cache.Fill(items);
        }

        _logger.LogDebug(@"Managed resource controller startup for type ""{type}"".", typeof(TEntity));
        _eventSubscription = Events.Subscribe();

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

    protected async Task HandleResourceEvent(ResourceEvent resourceEvent)
    {
        (ResourceEventType eventType, TEntity resource, _, _) = resourceEvent;

        _logger.LogDebug(
            @"Execute/Reconcile event ""{eventType}"" on resource ""{kind}/{name}"".",
            eventType,
            resource.Kind,
            resource.Name());

        var controllerType = _controllerRegistration.ControllerType;

        ResourceControllerResult? result = null;
        _logger.LogTrace(@"Instantiating new DI scope for controller ""{name}"".", controllerType.Name);
        using (var scope = _services.CreateScope())
        {
            if (scope.ServiceProvider.GetRequiredService(controllerType) is not IResourceController<TEntity>
                controller)
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
                switch (eventType)
                {
                    case ResourceEventType.Reconcile:
                        result = await controller.ReconcileAsync(resource);
                        _metrics.ReconciledEvents.Inc();
                        break;
                    case ResourceEventType.Deleted:
                        await controller.DeletedAsync(resource);
                        _metrics.DeletedEvents.Inc();
                        _logger.LogInformation(
                            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not possible.",
                            eventType,
                            resource.Kind,
                            resource.Name());
                        return;
                    case ResourceEventType.StatusUpdated:
                        await controller.StatusModifiedAsync(resource);
                        _metrics.StatusUpdatedEvents.Inc();
                        _logger.LogInformation(
                            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not possible.",
                            eventType,
                            resource.Kind,
                            resource.Name());
                        return;
                }
            }
            catch (Exception e)
            {
                RequeueError(resourceEvent, e);
                return;
            }
        }

        switch (result)
        {
            case null:
                _logger.LogInformation(
                    @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not requested.",
                    eventType,
                    resource.Kind,
                    resource.Name());
                return;
            case RequeueEventResult requeue:
                var specificQueueTypeRequested = requeue.EventType.HasValue;
                var requestedQueueType = requeue.EventType ?? (_settings.DefaultRequeueAsSameType
                    ? eventType
                    : ResourceEventType.Reconcile);

                if (specificQueueTypeRequested)
                {
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue requested as type ""{requeueType}"" with delay ""{requeue}"".",
                        eventType,
                        resource.Kind,
                        resource.Name(),
                        requestedQueueType,
                        requeue.RequeueIn);
                }
                else
                {
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue requested with delay ""{requeue}"".",
                        eventType,
                        resource.Kind,
                        resource.Name(),
                        requeue.RequeueIn);
                }

                RequeueDelayed(resourceEvent with { Type = requestedQueueType }, requeue.RequeueIn);
                break;
        }
    }

    protected async Task HandleResourceFinalization(ResourceEvent? resourceEvent)
    {
        using var scope = _services.CreateScope();

        if (resourceEvent == null)
        {
            return;
        }

        (_, TEntity resource, _, _) = resourceEvent;

        _logger.LogDebug(
            @"Finalize resource ""{kind}/{name}"".",
            resource.Kind,
            resource.Name());

        try
        {
            await scope.ServiceProvider.GetRequiredService<IFinalizerManager<TEntity>>()
                .FinalizeAsync(resourceEvent.Resource);
        }
        catch (Exception e)
        {
            RequeueError(resourceEvent, e);
        }
    }

    protected void RequeueError(ResourceEvent resourceEvent, Exception ex)
    {
        _metrics.ErroredEvents.Inc();

        var attempt = resourceEvent.Attempt + 1;
        var delay = _settings.ErrorBackoffStrategy(attempt);

        _logger.LogError(
            ex,
            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" threw an error. Retry attempt {retryAttempt}.",
            resourceEvent.Type,
            resourceEvent.Resource.Kind,
            resourceEvent.Resource.Name(),
            attempt);

        _localEvents.OnNext(resourceEvent with { Attempt = attempt, Delay = delay });
    }

    protected void RequeueDelayed(ResourceEvent resourceEvent, TimeSpan delay)
    {
        _metrics.RequeuedEvents.Inc();

        _localEvents.OnNext(resourceEvent with { Delay = delay });
    }

    private ResourceEvent MapCacheResult(
        CacheComparisonResult state,
        TEntity resource)
    {
        _logger.LogTrace(
            @"Mapping cache result ""{cacheResult}"" for ""{kind}/{name}"".",
            state,
            resource.Kind,
            resource.Name());

        ResourceEventType eventType;

        switch (state)
        {
            case CacheComparisonResult.Other when resource.Metadata.DeletionTimestamp != null:
                eventType = ResourceEventType.Finalizing;
                break;
            case CacheComparisonResult.Other:
                eventType = ResourceEventType.Reconcile;
                break;
            case CacheComparisonResult.StatusModified:
                eventType = ResourceEventType.StatusUpdated;
                break;
            case CacheComparisonResult.FinalizersModified:
                eventType = ResourceEventType.FinalizerModified;
                break;
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

        return new ResourceEvent(eventType, resource);
    }

    #nullable disable
    private IObservable<ResourceEvent> UpdateResourceData(
        ResourceEvent resourceEvent)
    {
        if (resourceEvent.Delay is null)
        {
            return Observable.Return(resourceEvent);
        }

        return Observable.FromAsync(
                async () =>
                {
                    ResourceEvent ret;
                    var resource = resourceEvent.Resource;

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
                        ret = null;
                    }
                    else
                    {
                        newResource = _cache.Upsert(newResource, out var state);
                        ret = MapCacheResult(state, newResource);
                    }

                    var updatedEvent = ret;

                    return updatedEvent;
                })
            .Where(e => e is not null);
    }
    #nullable enable

    private IObservable<Unit> HandleEvent(ResourceEvent resourceEvent)
    {
        return Observable.FromAsync(
            async () => await (resourceEvent.Type == ResourceEventType.Finalizing
                ? HandleResourceFinalization(resourceEvent)
                : HandleResourceEvent(resourceEvent)),
            ThreadPoolScheduler.Instance);
    }

    private ResourceEvent MapToResourceEvent(ResourceWatcher<TEntity>.WatchEvent watchEvent)
    {
        (WatchEventType watchEventType, TEntity resource) = watchEvent;
        ResourceEvent output;

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
                output = MapCacheResult(state, resource);
                break;
            case WatchEventType.Deleted:
                _cache.Remove(resource);
                output = new ResourceEvent(ResourceEventType.Deleted, resource);
                break;
            case WatchEventType.Error:
            case WatchEventType.Bookmark:
            default:
                var ex = new ArgumentException(
                    "The watcher event is not processable (only added / modified / deleted allowed).",
                    nameof(watchEvent));
                _logger.LogCritical(
                    ex,
                    @"The watcher event ""{watchEvent}"" is not further processable for the resource ""{kind}/{name}"".",
                    watchEventType,
                    resource.Kind,
                    resource.Name());

                throw ex;
        }

        _logger.LogTrace(
            @"Mapped watch event to ""{resourceEventType}"" for ""{kind}/{name}""",
            output.Type,
            output.Resource.Kind,
            output.Resource.Name());

        return output;
    }

    protected record ResourceEvent(ResourceEventType Type, TEntity Resource, int Attempt = 0, TimeSpan? Delay = null);
}
