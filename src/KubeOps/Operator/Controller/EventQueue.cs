using System.Reactive.Linq;
using System.Reactive.Subjects;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Kubernetes;

namespace KubeOps.Operator.Controller;

internal class EventQueue<TEntity> : IEventQueue<TEntity>
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    private readonly IKubernetesClient _kubernetesClient;
    private readonly ILogger<EventQueue<TEntity>> _logger;
    private readonly OperatorSettings _operatorSettings;
    private readonly IResourceCache<TEntity> _resourceCache;
    private readonly IResourceWatcher<TEntity> _watcher;

    private readonly Subject<ResourceEvent<TEntity>> _localEvents;
    private Action<ResourceEvent<TEntity>> _onWatcherEvent;

    public EventQueue(
        IKubernetesClient kubernetesClient,
        ILogger<EventQueue<TEntity>> logger,
        OperatorSettings operatorSettings,
        IResourceCache<TEntity> resourceCache,
        IResourceWatcher<TEntity> watcher)
    {
        _kubernetesClient = kubernetesClient;
        _logger = logger;
        _operatorSettings = operatorSettings;
        _resourceCache = resourceCache;
        _watcher = watcher;

        _localEvents = new Subject<ResourceEvent<TEntity>>();
        _onWatcherEvent = _ => { };

        var watcherEvents = _watcher
            .WatchEvents
            .Select(MapToResourceEvent)
            .Do(_onWatcherEvent);

        Events = _localEvents
            .Merge(watcherEvents)
            .Where(EventRetryCountIsLessThanMax)
            .GroupBy(e => e.Resource.Uid())
            .Select(
                group => group
                    .Select(ProcessDelay)
                    .Switch())
            .Merge()
            .Select(UpdateResourceData)
            .Merge()
            .Where(EventTypeIsNotFinalizerModified);
    }

    public IObservable<ResourceEvent<TEntity>> Events { get; }

    public async Task StartAsync(Action<ResourceEvent<TEntity>> onWatcherEvent)
    {
        if (_operatorSettings.PreloadCache)
        {
            _logger.LogInformation("The 'preload cache' setting is set to 'true'.");
            var items = await _kubernetesClient.List<TEntity>(_operatorSettings.Namespace);
            _resourceCache.Fill(items);
        }

        _onWatcherEvent = onWatcherEvent;

        await _watcher.StartAsync();
    }

    public async Task StopAsync()
    {
        await _watcher.StopAsync();
    }

    public void EnqueueLocal(ResourceEvent<TEntity> resourceEvent) => _localEvents.OnNext(resourceEvent);

    private static bool EventTypeIsNotFinalizerModified(ResourceEvent<TEntity> resourceEvent) =>
        resourceEvent.Type != ResourceEventType.FinalizerModified;

    private static IObservable<ResourceEvent<TEntity>> ProcessDelay(
        ResourceEvent<TEntity> resourceEvent)
    {
        var delay = resourceEvent.Delay ?? TimeSpan.Zero;
        return Observable.Return(resourceEvent).DelaySubscription(delay);
    }

    private bool EventRetryCountIsLessThanMax(ResourceEvent<TEntity> resourceEvent)
    {
        (ResourceEventType type, TEntity resource, var attempt, TimeSpan? delay) = resourceEvent;

        if (attempt == 0)
        {
            return true;
        }

        if (attempt <= _operatorSettings.MaxErrorRetries)
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

    private ResourceEvent<TEntity> MapToResourceEvent(ResourceWatcher<TEntity>.WatchEvent watchEvent)
    {
        (WatchEventType watchEventType, TEntity resource) = watchEvent;
        ResourceEvent<TEntity> output;

        _logger.LogTrace(
            @"Mapping watcher event ""{watchEvent}"" for ""{kind}/{name}"".",
            watchEventType,
            resource.Kind,
            resource.Name());

        switch (watchEventType)
        {
            case WatchEventType.Added:
            case WatchEventType.Modified:
                resource = _resourceCache.Upsert(resource, out var state);
                output = MapCacheResult(state, resource);
                break;
            case WatchEventType.Deleted:
                _resourceCache.Remove(resource);
                output = new ResourceEvent<TEntity>(ResourceEventType.Deleted, resource);
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

    private ResourceEvent<TEntity> MapCacheResult(
        CacheComparisonResult state,
        TEntity resource)
    {
        _logger.LogTrace(
            @"Mapping cache result ""{cacheResult}"" for ""{kind}/{name}"".",
            state,
            resource.Kind,
            resource.Name());

        var eventType = (state, resource.Metadata.DeletionTimestamp) switch
        {
            (CacheComparisonResult.Other, { }) => ResourceEventType.Finalizing,
            (CacheComparisonResult.Other, _) => ResourceEventType.Reconcile,
            (CacheComparisonResult.StatusModified, _) => ResourceEventType.StatusUpdated,
            (CacheComparisonResult.FinalizersModified, _) => ResourceEventType.FinalizerModified,
            _ => throw new ArgumentException("The caching state is out of the processable range", nameof(state)),
        };

        return new ResourceEvent<TEntity>(eventType, resource);
    }

#nullable disable
    private IObservable<ResourceEvent<TEntity>> UpdateResourceData(
        ResourceEvent<TEntity> resourceEvent)
    {
        if (resourceEvent.Delay is null)
        {
            return Observable.Return(resourceEvent);
        }

        return Observable.FromAsync(
                async () =>
                {
                    ResourceEvent<TEntity> ret;
                    var resource = resourceEvent.Resource;

                    _logger.LogTrace(
                        @"Update resource from k8s / cache for delayed requeue for ""{kind}/{name}"".",
                        resource.Kind,
                        resource.Name());

                    var newResource = await _kubernetesClient.Get<TEntity>(
                        resource.Name(),
                        resource.Namespace());

                    if (newResource == null)
                    {
                        _resourceCache.Remove(resource);
                        _logger.LogDebug(
                            @"Resource ""{kind}/{name}"" for enqueued event was not present anymore.",
                            resource.Kind,
                            resource.Name());
                        ret = null;
                    }
                    else
                    {
                        newResource = _resourceCache.Upsert(newResource, out var state);
                        ret = MapCacheResult(state, newResource);
                    }

                    var updatedEvent = ret;

                    return updatedEvent;
                })
            .Where(e => e is not null);
    }
#nullable enable
}
