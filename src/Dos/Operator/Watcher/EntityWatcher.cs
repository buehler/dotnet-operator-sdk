using System;
using System.Threading;
using System.Threading.Tasks;
using Dos.Operator.Client;
using Dos.Operator.DependencyInjection;
using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dos.Operator.Watcher
{
    internal class EntityWatcher<TEntity> : IDisposable
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly ILogger<EntityWatcher<TEntity>> _logger;
        private CancellationTokenSource? _cancellation;
        private Watcher<TEntity>? _watcher;

        private readonly Lazy<IKubernetesClient> _client =
            new Lazy<IKubernetesClient>(() => DependencyInjector.Services.GetRequiredService<IKubernetesClient>());

        public event EventHandler<(WatchEventType type, TEntity resource)>? WatcherEvent;

        public EntityWatcher()
        {
            _logger = DependencyInjector.Services.GetRequiredService<ILogger<EntityWatcher<TEntity>>>();
        }

        public Task Start()
        {
            _logger.LogTrace(@"Resource Watcher startup for type ""{type}"".", typeof(TEntity));
            return WatchResource();
        }

        public void Stop()
        {
            _logger.LogTrace(@"Resource Watcher shutdown for type ""{type}"".", typeof(TEntity));
            _cancellation?.Cancel();
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
                    _logger.LogDebug(@"Watcher for type ""{type}"" already running.", typeof(TEntity));
                    return;
                }
            }

            _cancellation = new CancellationTokenSource();
            // TODO: namespaced resources
            _watcher = await _client.Value.Watch<TEntity>(
                TimeSpan.FromHours(1),
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
            _logger.LogError(e, @"There was an error while watching the resource ""{resource}"".", typeof(TEntity));
            // _logger.LogInformation("Trying to reconnect.");
            // RestartWatcher();
            _cancellation?.Cancel();
            _watcher?.Dispose();
            _watcher = null;
        }

        private void OnClose()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _logger.LogInformation("The server closed the connection. Trying to reconnect.");
                RestartWatcher();
            }
        }
    }
}
