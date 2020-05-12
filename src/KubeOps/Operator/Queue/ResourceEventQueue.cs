using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Errors;
using KubeOps.Operator.Watcher;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Queue
{
    internal class ResourceEventQueue<TEntity> : IResourceEventQueue<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        // TODO: Make configurable
        private const int QueueLimit = 512;

        private readonly Channel<(ResourceEventType type, TEntity resource)> _queue =
            Channel.CreateBounded<(ResourceEventType type, TEntity resource)>(QueueLimit);

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly ILogger<ResourceEventQueue<TEntity>> _logger;
        private readonly IResourceCache<TEntity> _cache;
        private readonly IResourceWatcher<TEntity> _watcher;

        private readonly IDictionary<string, ResourceTimer<TEntity>> _delayedEnqueue =
            new ConcurrentDictionary<string, ResourceTimer<TEntity>>();

        private readonly ConcurrentDictionary<string, ExponentialBackoffHandler> _errorHandlers =
            new ConcurrentDictionary<string, ExponentialBackoffHandler>();

        private CancellationTokenSource? _cancellation;

        public event EventHandler<(ResourceEventType type, TEntity resource)>? ResourceEvent;

        public ResourceEventQueue(
            ILogger<ResourceEventQueue<TEntity>> logger,
            IResourceCache<TEntity> cache,
            IResourceWatcher<TEntity> watcher)
        {
            _logger = logger;
            _cache = cache;
            _watcher = watcher;
        }

        public async Task Start()
        {
            _logger.LogTrace(@"Event queue startup for type ""{type}"".", typeof(TEntity));
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
        }

        public void Dispose()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
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
                ResourceEvent -= (EventHandler<(ResourceEventType type, TEntity resource)>) handler;
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
                            _delayedEnqueue.Remove(delayedResource.Metadata.Uid);
                            var cachedResource = _cache.Get(delayedResource.Metadata.Uid);
                            if (cachedResource == null)
                            {
                                _logger.LogDebug(
                                    @"Resource ""{kind}/{name}"" was not present in the cache anymore. Don't execute delayed timer.",
                                    delayedResource.Kind,
                                    delayedResource.Metadata.Name);
                                return;
                            }

                            await Enqueue(cachedResource);
                        });
                    _delayedEnqueue.Add(resource.Metadata.Uid, timer);

                    _logger.LogDebug(
                        @"Enqueued delayed ({delay}) event for ""{kind}/{name}"".",
                        enqueueDelay.Value,
                        resource.Kind,
                        resource.Metadata.Name);
                    timer.Start();

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
                        await EnqueueEvent(ResourceEventType.Finalizing, resource);
                        break;
                    case CacheComparisonResult.New:
                        await EnqueueEvent(ResourceEventType.Created, resource);
                        break;
                    case CacheComparisonResult.Modified:
                        await EnqueueEvent(ResourceEventType.Updated, resource);
                        break;
                    case CacheComparisonResult.StatusModified:
                        await EnqueueEvent(ResourceEventType.StatusUpdated, resource);
                        break;
                    case CacheComparisonResult.NotModified:
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
            if (_errorHandlers.Remove(resource.Metadata.Uid, out var handler))
            {
                handler.Dispose();
            }
        }

        private async void OnWatcherEvent(object? _, (WatchEventType type, TEntity resource) args)
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
        }

        private async Task ReadQueue()
        {
            _logger.LogTrace(@"Start queue reader for type ""{type}"".", typeof(TEntity));

            while (_cancellation != null &&
                   !_cancellation.IsCancellationRequested &&
                   await _queue.Reader.WaitToReadAsync(_cancellation.Token))
            {
                if (!_queue.Reader.TryRead(out var message))
                {
                    continue;
                }

                _logger.LogTrace(
                    @"Read event ""{type}"" for resource ""{resource}"".",
                    message.type,
                    message.resource);
                ResourceEvent?.Invoke(this, message);
            }
        }
    }
}
