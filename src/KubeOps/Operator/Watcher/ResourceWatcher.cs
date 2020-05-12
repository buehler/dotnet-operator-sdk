using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Client;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace KubeOps.Operator.Watcher
{
    internal class ResourceWatcher<TEntity> : IResourceWatcher<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private const double MaxRetrySeconds = 64;

        private int _errorCount;

        private readonly ILogger<ResourceWatcher<TEntity>> _logger;
        private readonly IKubernetesClient _client;

        private readonly Random _rnd = new Random();
        private CancellationTokenSource? _cancellation;
        private Watcher<TEntity>? _watcher;

        private Timer? _reconnectTimer;
        private Timer? _resetErrCountTimer;

        public event EventHandler<(WatchEventType type, TEntity resource)>? WatcherEvent;

        public ResourceWatcher(ILogger<ResourceWatcher<TEntity>> logger, IKubernetesClient client)
        {
            _logger = logger;
            _client = client;
        }

        public Task Start()
        {
            _logger.LogTrace(@"Resource Watcher startup for type ""{type}"".", typeof(TEntity));
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

            _reconnectTimer?.Dispose();
            _resetErrCountTimer?.Dispose();
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

            _resetErrCountTimer = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
            _resetErrCountTimer.Elapsed += (_, __) =>
            {
                _logger.LogTrace("Reset error count in resource watcher.");
                _errorCount = 0;
                _resetErrCountTimer.Dispose();
                _resetErrCountTimer = null;
                _reconnectTimer?.Stop();
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;
            };
            _resetErrCountTimer.Start();

            _cancellation = new CancellationTokenSource();
            // TODO: namespaced resources
            _watcher = await _client.Watch<TEntity>(
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
            _cancellation?.Cancel();
            _watcher?.Dispose();
            _watcher = null;

            _logger.LogInformation("Trying to reconnect with exponential backoff.");
            _resetErrCountTimer?.Stop();
            _resetErrCountTimer?.Dispose();
            _resetErrCountTimer = null;
            _reconnectTimer?.Stop();
            _reconnectTimer?.Dispose();
            _reconnectTimer = new Timer(ExponentialBackoff(++_errorCount).TotalMilliseconds);
            _reconnectTimer.Elapsed += (_, __) => RestartWatcher();
            _reconnectTimer.Start();
        }

        private void OnClose()
        {
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _logger.LogInformation("The server closed the connection. Trying to reconnect.");
                RestartWatcher();
            }
        }

        private TimeSpan ExponentialBackoff(int retryCount) => TimeSpan
            .FromSeconds(Math.Min(Math.Pow(2, retryCount), MaxRetrySeconds))
            .Add(TimeSpan.FromMilliseconds(_rnd.Next(0, 1000)));
    }
}
