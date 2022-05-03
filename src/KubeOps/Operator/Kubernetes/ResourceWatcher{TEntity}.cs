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

namespace KubeOps.Operator.Kubernetes;

internal class ResourceWatcher<TEntity> : IDisposable
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly Subject<WatchEvent> _watchEvents = new();
    private readonly IKubernetesClient _client;
    private readonly ILogger<ResourceWatcher<TEntity>> _logger;
    private readonly ResourceWatcherMetrics<TEntity> _metrics;
    private readonly OperatorSettings _settings;
    private readonly Subject<TimeSpan> _reconnectHandler = new();
    private readonly IDisposable _reconnectSubscription;

    private IDisposable? _resetReconnectCounter;
    private int _reconnectAttempts;
    private CancellationTokenSource? _cancellation;
    private Watcher<TEntity>? _watcher;

    public ResourceWatcher(
        IKubernetesClient client,
        ILogger<ResourceWatcher<TEntity>> logger,
        ResourceWatcherMetrics<TEntity> metrics,
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

    public IObservable<WatchEvent> WatchEvents => _watchEvents;

    public Task StartAsync()
    {
        _logger.LogDebug(@"Resource Watcher startup for type ""{type}"".", typeof(TEntity));
        return WatchResource();
    }

    public Task StopAsync()
    {
        _logger.LogTrace(@"Resource Watcher shutdown for type ""{type}"".", typeof(TEntity));
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
        _logger.LogTrace(@"Disposed resource watcher for type ""{type}"".", typeof(TEntity));
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
                _logger.LogTrace(@"Watcher for type ""{type}"" already running.", typeof(TEntity));
                return;
            }
        }

        _cancellation = new CancellationTokenSource();

        _watcher = await _client.Watch<TEntity>(
            TimeSpan.FromSeconds(_settings.WatcherHttpTimeout),
            OnWatcherEvent,
            OnException,
            OnClose,
            _settings.Namespace,
            _cancellation.Token);
        _metrics.Running.Set(1);
    }

    private void OnWatcherEvent(WatchEventType type, TEntity resource)
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
                _watchEvents.OnNext(new WatchEvent(type, resource));
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
        _logger.LogTrace(@"Restarting resource watcher for type ""{type}"".", typeof(TEntity));
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
                typeof(TEntity));
            WatchResource().ConfigureAwait(false);
            return;
        }

        _logger.LogError(e, @"There was an error while watching the resource ""{resource}"".", typeof(TEntity));
        var backoff = _settings.ErrorBackoffStrategy(++_reconnectAttempts);
        if (backoff.TotalSeconds > _settings.WatcherMaxRetrySeconds)
        {
            backoff = TimeSpan.FromSeconds(_settings.WatcherMaxRetrySeconds);
        }

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

    internal record WatchEvent(WatchEventType Type, TEntity Resource);
}
