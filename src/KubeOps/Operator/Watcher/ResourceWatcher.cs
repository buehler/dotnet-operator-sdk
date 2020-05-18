using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Client;
using KubeOps.Operator.Errors;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Watcher
{
    internal class ResourceWatcher<TEntity> : IResourceWatcher<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly ILogger<ResourceWatcher<TEntity>> _logger;
        private readonly IKubernetesClient _client;
        private readonly ExponentialBackoffHandler _reconnectHandler;

        private CancellationTokenSource? _cancellation;
        private Watcher<TEntity>? _watcher;

        public event EventHandler<(WatchEventType type, TEntity resource)>? WatcherEvent;

        public ResourceWatcher(ILogger<ResourceWatcher<TEntity>> logger, IKubernetesClient client)
        {
            _logger = logger;
            _client = client;
            _reconnectHandler = new ExponentialBackoffHandler(async () => await WatchResource());
        }

        public Task Start()
        {
            _logger.LogDebug(@"Resource Watcher startup for type ""{type}"".", typeof(TEntity));
            return WatchResource();
        }

        public Task Stop()
        {
            _logger.LogTrace(@"Resource Watcher shutdown for type ""{type}"".", typeof(TEntity));
            _cancellation?.Cancel();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
            }

            foreach (var handler in WatcherEvent?.GetInvocationList() ?? new Delegate[] { })
            {
                WatcherEvent -= (EventHandler<(WatchEventType type, TEntity resource)>) handler;
            }

            _reconnectHandler.Dispose();
            _cancellation?.Dispose();
            _watcher?.Dispose();
            _logger.LogTrace(@"Disposed resource watcher for type ""{type}"".", typeof(TEntity));
        }

        private async Task WatchResource()
        {
            if (_watcher != null)
            {
                if (!_watcher.Watching)
                {
                    _watcher.Dispose();
                }
                else
                {
                    _logger.LogTrace(@"Watcher for type ""{type}"" already running.", typeof(TEntity));
                    return;
                }
            }

            _cancellation = new CancellationTokenSource();
            // TODO: namespaced resources
            _watcher = await _client.Watch<TEntity>(
                TimeSpan.FromMinutes(1),
                OnWatcherEvent,
                OnException,
                OnClose,
                null,
                _cancellation.Token);
        }

        private async void RestartWatcher()
        {
            _logger.LogTrace(@"Restarting resource watcher for type ""{type}"".", typeof(TEntity));
            _cancellation?.Cancel();
            _watcher?.Dispose();
            _watcher = null;
            await WatchResource();
        }

        private void OnWatcherEvent(WatchEventType type, TEntity resource)
        {
            _logger.LogTrace(
                @"Received watch event ""{eventType}"" for ""{kind}/{name}"".",
                type,
                resource.Kind,
                resource.Metadata.Name);

            switch (type)
            {
                case WatchEventType.Added:
                case WatchEventType.Modified:
                case WatchEventType.Deleted:
                    WatcherEvent?.Invoke(this, (type, resource));
                    break;
                case WatchEventType.Error:
                case WatchEventType.Bookmark:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Event did not match.");
            }
        }

        private void OnException(Exception e)
        {
            _cancellation?.Cancel();
            _watcher?.Dispose();
            _watcher = null;

            if (e is TaskCanceledException && e.InnerException is IOException)
            {
                _logger.LogTrace(
                    @"Either the server or the client did close the connection on watcher for resource ""{resource}"". Restart.",
                    typeof(TEntity));
                WatchResource().ConfigureAwait(false);
                return;
            }

            _logger.LogError(e, @"There was an error while watching the resource ""{resource}"".", typeof(TEntity));
            var backoff = _reconnectHandler.Retry(TimeSpan.FromSeconds(5));
            _logger.LogInformation("Trying to reconnect with exponential backoff {backoff}.", backoff);
        }

        private void OnClose()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _logger.LogDebug("The server closed the connection. Trying to reconnect.");
                RestartWatcher();
            }
        }
    }
}
