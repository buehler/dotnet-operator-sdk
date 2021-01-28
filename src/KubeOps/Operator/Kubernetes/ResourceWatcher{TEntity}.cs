using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.DevOps;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Kubernetes
{
    internal class ResourceWatcher<TResource> : IDisposable
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private const double MaxRetrySeconds = 32;

        private readonly Subject<(WatchEventType Event, TResource Resource)> _watchEvents = new();
        private readonly IKubernetesClient _client;
        private readonly ILogger<ResourceWatcher<TResource>> _logger;
        private readonly ResourceWatcherMetrics<TResource> _metrics;
        private readonly OperatorSettings _settings;
        private readonly Subject<TimeSpan> _reconnectHandler = new();
        private readonly IDisposable _reconnectSubscription;
        private readonly Random _rnd = new();

        private IDisposable? _resetReconnectCounter;
        private int _reconnectAttempts;
        private CancellationTokenSource? _cancellation;
        private Watcher<TResource>? _watcher;

        public ResourceWatcher(
            IKubernetesClient client,
            ILogger<ResourceWatcher<TResource>> logger,
            ResourceWatcherMetrics<TResource> metrics,
            OperatorSettings settings)
        {
            _client = client;
            _logger = logger;
            _metrics = metrics;
            _settings = settings;
            _reconnectSubscription =
                _reconnectHandler
                    .Select(Observable.Timer)
                    .Switch()
                    .Subscribe(async _ => await WatchResource());
        }

        public IObservable<(WatchEventType Event, TResource Resource)> WatchEvents => _watchEvents;

        public Task Start()
        {
            _logger.LogDebug(@"Resource Watcher startup for type ""{type}"".", typeof(TResource));
            return WatchResource();
        }

        public Task Stop()
        {
            _logger.LogTrace(@"Resource Watcher shutdown for type ""{type}"".", typeof(TResource));
            Disposing(true);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Disposing(false);
        }

        private void Disposing(bool fromStop)
        {
            if (!fromStop)
            {
                _watchEvents.Dispose();
                _reconnectHandler.Dispose();
            }

            _reconnectHandler.Dispose();
            _reconnectSubscription.Dispose();
            if (_cancellation?.IsCancellationRequested == false)
            {
                _cancellation.Cancel();
            }

            _cancellation?.Dispose();
            _watcher?.Dispose();
            _logger.LogTrace(@"Disposed resource watcher for type ""{type}"".", typeof(TResource));
            _metrics.Running.Set(0);
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
                    _logger.LogTrace(@"Watcher for type ""{type}"" already running.", typeof(TResource));
                    return;
                }
            }

            _cancellation = new CancellationTokenSource();

            _watcher = await _client.Watch<TResource>(
                TimeSpan.FromSeconds(_settings.WatcherHttpTimeout),
                OnWatcherEvent,
                OnException,
                OnClose,
                _settings.Namespace,
                _cancellation.Token);
            _metrics.Running.Set(1);
        }

        private void OnWatcherEvent(WatchEventType type, TResource resource)
        {
            _logger.LogTrace(
                @"Received watch event ""{eventType}"" for ""{kind}/{name}"".",
                type,
                resource.Kind,
                resource.Metadata.Name);

            _metrics.WatchedEvents.Inc();

            switch (type)
            {
                case WatchEventType.Added:
                case WatchEventType.Modified:
                case WatchEventType.Deleted:
                    _watchEvents.OnNext((type, resource));
                    break;
                case WatchEventType.Error:
                case WatchEventType.Bookmark:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Event did not match.");
            }
        }

        private async void RestartWatcher()
        {
            _logger.LogTrace(@"Restarting resource watcher for type ""{type}"".", typeof(TResource));
            _cancellation?.Cancel();
            _watcher?.Dispose();
            _watcher = null;
            await WatchResource();
        }

        private void OnException(Exception e)
        {
            _cancellation?.Cancel();
            _watcher?.Dispose();
            _watcher = null;

            _metrics.Running.Set(0);
            _metrics.WatcherExceptions.Inc();

            if (e is TaskCanceledException && e.InnerException is IOException)
            {
                _logger.LogTrace(
                    @"Either the server or the client did close the connection on watcher for resource ""{resource}"". Restart.",
                    typeof(TResource));
                WatchResource().ConfigureAwait(false);
                return;
            }

            _logger.LogError(e, @"There was an error while watching the resource ""{resource}"".", typeof(TResource));
            var backoff = ExponentialBackoff(++_reconnectAttempts);
            _logger.LogInformation("Trying to reconnect with exponential backoff {backoff}.", backoff);
            _resetReconnectCounter?.Dispose();
            _resetReconnectCounter = Observable
                .Timer(TimeSpan.FromMinutes(1))
                .FirstAsync()
                .Subscribe(_ => _reconnectAttempts = 0);

            _reconnectHandler.OnNext(backoff);
        }

        private void OnClose()
        {
            _metrics.Running.Set(0);
            _metrics.WatcherClosed.Inc();

            if (_cancellation?.IsCancellationRequested == false)
            {
                _logger.LogDebug("The server closed the connection. Trying to reconnect.");
                RestartWatcher();
            }
        }

        private TimeSpan ExponentialBackoff(int retryCount) => TimeSpan
            .FromSeconds(Math.Min(Math.Pow(2, retryCount), MaxRetrySeconds))
            .Add(TimeSpan.FromMilliseconds(_rnd.Next(0, 1000)));
    }
}
