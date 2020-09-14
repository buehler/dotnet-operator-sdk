using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Client;
using KubeOps.Operator.Controller;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Errors;
using KubeOps.Operator.Watcher;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Queue
{
    internal class ResourceEventQueue<TEntity> : IResourceEventQueue<TEntity>
        where TEntity : class, IKubernetesObject<V1ObjectMeta>
    {
        private const int QueueLimit = 512;

        private readonly Channel<(ResourceEventType Type, TEntity Resource)> _queue =
            Channel.CreateBounded<(ResourceEventType Type, TEntity Resource)>(QueueLimit);

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly ILogger<ResourceEventQueue<TEntity>> _logger;
        private readonly IKubernetesClient _client;
        private readonly IResourceCache<TEntity> _cache;
        private readonly IResourceWatcher<TEntity> _watcher;

        private readonly IDictionary<string, ResourceTimer<TEntity>> _delayedEnqueue =
            new ConcurrentDictionary<string, ResourceTimer<TEntity>>();

        private readonly ConcurrentDictionary<string, ExponentialBackoffHandler> _errorHandlers =
            new ConcurrentDictionary<string, ExponentialBackoffHandler>();

        private readonly ResourceEventQueueMetrics<TEntity> _metrics;

        private int _queueSize;
        private CancellationTokenSource? _cancellation;

        public ResourceEventQueue(
            ILogger<ResourceEventQueue<TEntity>> logger,
            IKubernetesClient client,
            IResourceCache<TEntity> cache,
            IResourceWatcher<TEntity> watcher,
            OperatorSettings settings)
        {
            _logger = logger;
            _client = client;
            _cache = cache;
            _watcher = watcher;
            _metrics = new ResourceEventQueueMetrics<TEntity>(settings);
        }

        public event EventHandler<(ResourceEventType Type, TEntity Resource)>? ResourceEvent;

        public async Task Start()
        {
            _logger.LogDebug(@"Event queue startup for type ""{type}"".", typeof(TEntity));
            _cancellation ??= new CancellationTokenSource();
            _watcher.WatcherEvent += OnWatcherEvent;
            await _watcher.Start();
#pragma warning disable 4014
            Task.Run(async () => await ReadQueue(), _cancellation.Token).ConfigureAwait(false);
#pragma warning restore 4014
        }

        public async Task Stop()
        {
            _logger.LogTrace(@"Event queue shutdown for type ""{type}"".", typeof(TEntity));
            _cancellation?.Cancel();
            await _watcher.Stop();
            _watcher.WatcherEvent -= OnWatcherEvent;
            foreach (var timer in _delayedEnqueue.Values)
            {
                timer.Destroy();
            }

            foreach (var errorBackoff in _errorHandlers.Values)
            {
                errorBackoff.Dispose();
            }

            _delayedEnqueue.Clear();
            _errorHandlers.Clear();
            _cache.Clear();
            _metrics.Running.Set(0);
        }

        public void Dispose()
        {
            if (_cancellation?.IsCancellationRequested == false)
            {
                _cancellation.Cancel();
            }

            _queue.Writer.Complete();
            _watcher.Dispose();
            _cache.Clear();
            foreach (var timer in _delayedEnqueue.Values)
            {
                timer.Destroy();
            }

            _delayedEnqueue.Clear();
            foreach (var handler in ResourceEvent?.GetInvocationList() ?? new Delegate[] { })
            {
                ResourceEvent -= (EventHandler<(ResourceEventType Type, TEntity Resource)>)handler;
            }

            foreach (var errorBackoff in _errorHandlers.Values)
            {
                errorBackoff.Dispose();
            }
        }

        public async Task Enqueue(TEntity resource, TimeSpan? enqueueDelay = null)
        {
            try
            {
                await _semaphore.WaitAsync();
                if (enqueueDelay != null && enqueueDelay != TimeSpan.Zero)
                {
                    var timer = new ResourceTimer<TEntity>(
                        resource,
                        enqueueDelay.Value,
                        async delayedResource =>
                        {
                            _logger.LogTrace(
                                @"Delayed event timer elapsed for ""{kind}/{name}"".",
                                delayedResource.Kind,
                                delayedResource.Metadata.Name);
                            try
                            {
                                await _semaphore.WaitAsync();
                                _delayedEnqueue.Remove(delayedResource.Metadata.Uid);
                                _metrics.DelayedQueueSize.Set(_delayedEnqueue.Count);
                                _metrics.DelayedQueueSizeSummary.Observe(_delayedEnqueue.Count);
                            }
                            finally
                            {
                                _semaphore.Release();
                            }

                            // TODO: is this really necessary?
                            var newResource = await _client.Get<TEntity>(
                                delayedResource.Metadata.Name,
                                delayedResource.Metadata.NamespaceProperty);

                            if (newResource == null)
                            {
                                _logger.LogDebug(
                                    @"Resource ""{kind}/{name}"" for enqueued event was not present anymore.",
                                    resource.Kind,
                                    resource.Metadata.Name);
                                return;
                            }

                            await Enqueue(newResource);
                        });
                    _delayedEnqueue.Add(resource.Metadata.Uid, timer);

                    _logger.LogDebug(
                        @"Enqueued delayed ({delay}) event for ""{kind}/{name}"".",
                        enqueueDelay.Value,
                        resource.Kind,
                        resource.Metadata.Name);
                    timer.Start();

                    _metrics.RequeuedEvents.Inc();
                    _metrics.DelayedQueueSize.Set(_delayedEnqueue.Count);
                    _metrics.DelayedQueueSizeSummary.Observe(_delayedEnqueue.Count);

                    return;
                }

                resource = _cache.Upsert(resource, out var state);
                _logger.LogTrace(
                    @"Resource ""{kind}/{name}"" comparison result ""{comparisonResult}"".",
                    resource.Kind,
                    resource.Metadata.Name,
                    state);

                switch (state)
                {
                    case CacheComparisonResult.New when resource.Metadata.DeletionTimestamp != null:
                    case CacheComparisonResult.Modified when resource.Metadata.DeletionTimestamp != null:
                    case CacheComparisonResult.NotModified when resource.Metadata.DeletionTimestamp != null:
                        _metrics.FinalizingEvents.Inc();
                        await EnqueueEvent(ResourceEventType.Finalizing, resource);
                        break;
                    case CacheComparisonResult.New:
                        _metrics.CreatedEvents.Inc();
                        await EnqueueEvent(ResourceEventType.Created, resource);
                        break;
                    case CacheComparisonResult.Modified:
                        _metrics.UpdatedEvents.Inc();
                        await EnqueueEvent(ResourceEventType.Updated, resource);
                        break;
                    case CacheComparisonResult.StatusModified:
                        _metrics.StatusUpdatedEvents.Inc();
                        await EnqueueEvent(ResourceEventType.StatusUpdated, resource);
                        break;
                    case CacheComparisonResult.NotModified:
                        _metrics.NotModifiedEvents.Inc();
                        await EnqueueEvent(ResourceEventType.NotModified, resource);
                        break;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void EnqueueErrored(ResourceEventType type, TEntity resource)
        {
            _metrics.ErroredEvents.Inc();
            var handler = _errorHandlers.GetOrAdd(
                resource.Metadata.Uid,
                _ =>
                {
                    return new ExponentialBackoffHandler(
                        async () =>
                        {
                            _logger.LogTrace(
                                @"Backoff (error) requeue timer elapsed for ""{kind}/{name}"".",
                                resource.Kind,
                                resource.Metadata.Name);
                            await EnqueueEvent(type, resource);
                        });
                });
            _metrics.ErrorQueueSize.Set(_errorHandlers.Count);
            _metrics.ErrorQueueSizeSummary.Observe(_errorHandlers.Count);

            var backoff = handler.Retry();
            _logger.LogDebug(
                @"Requeue event ""{eventType}"" with backoff ""{backoff}"" for resource ""{kind}/{name}"".",
                type,
                backoff,
                resource.Kind,
                resource.Metadata.Name);
        }

        public void ClearError(TEntity resource)
        {
            if (!_errorHandlers.Remove(resource.Metadata.Uid, out var handler))
            {
                return;
            }

            _metrics.ErrorQueueSize.Set(_errorHandlers.Count);
            _metrics.ErrorQueueSizeSummary.Observe(_errorHandlers.Count);
            handler.Dispose();
        }

        private async void OnWatcherEvent(object? _, (WatchEventType Type, TEntity Resource) args)
        {
            var (type, resource) = args;
            switch (type)
            {
                case WatchEventType.Added:
                case WatchEventType.Modified:
                    await Enqueue(resource);
                    break;
                case WatchEventType.Deleted:
                    await EnqueueDeleted(resource);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task EnqueueDeleted(TEntity resource)
        {
            _logger.LogTrace(
                @"Resource ""{kind}/{name}"" was deleted.",
                resource.Kind,
                resource.Metadata.Name);
            try
            {
                await _semaphore.WaitAsync();
                _cache.Remove(resource);
            }
            finally
            {
                _semaphore.Release();
            }

            _metrics.DeletedEvents.Inc();
            await EnqueueEvent(ResourceEventType.Deleted, resource);
        }

        private async Task EnqueueEvent(ResourceEventType type, TEntity resource)
        {
            _logger.LogTrace(
                @"Enqueue event ""{type}"" for resource ""{kind}/{name}"".",
                type,
                resource.Kind,
                resource.Metadata.Name);
            _cancellation ??= new CancellationTokenSource();

            if (_delayedEnqueue.TryGetValue(resource.Metadata.Uid, out var timer))
            {
                _logger.LogDebug(
                    @"Event ""{type}"" for resource ""{kind}/{name}"" already had a delayed timer. Destroy the timer.",
                    type,
                    resource.Kind,
                    resource.Metadata.Name);
                timer.Destroy();
                _delayedEnqueue.Remove(resource.Metadata.Uid);
            }

            await _queue.Writer.WaitToWriteAsync(_cancellation.Token);
            if (!_queue.Writer.TryWrite((type, resource)))
            {
                _logger.LogWarning(
                    @"Queue for type ""{type}"" could not write into output channel.",
                    typeof(TEntity));
            }

            _metrics.WrittenQueueEvents.Inc();
            _metrics.QueueSizeSummary.Observe(++_queueSize);
            _metrics.QueueSize.Set(_queueSize);
        }

        private async Task ReadQueue()
        {
            _logger.LogTrace(@"Start queue reader for type ""{type}"".", typeof(TEntity));

            _metrics.Running.Set(1);
            while (_cancellation?.IsCancellationRequested == false &&
                   await _queue.Reader.WaitToReadAsync(_cancellation.Token))
            {
                if (!_queue.Reader.TryRead(out var message))
                {
                    continue;
                }

                _logger.LogTrace(
                    @"Read event ""{type}"" for resource ""{resource}"".",
                    message.Type,
                    message.Resource);
                _metrics.ReadQueueEvents.Inc();
                _metrics.QueueSizeSummary.Observe(--_queueSize);
                _metrics.QueueSize.Set(_queueSize);
                ResourceEvent?.Invoke(this, message);
            }
        }
    }
}
