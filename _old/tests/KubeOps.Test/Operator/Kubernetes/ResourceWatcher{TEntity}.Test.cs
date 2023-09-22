using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text.Json;

using FluentAssertions;

using k8s;
using k8s.Models;

using KubeOps.KubernetesClient;
using KubeOps.Operator;
using KubeOps.Operator.Caching;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Kubernetes;
using KubeOps.Test.TestEntities;
using KubeOps.TestOperator.Entities;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Reactive.Testing;

using Moq;

using Prometheus;

using Xunit;

namespace KubeOps.Test.Operator.Kubernetes;

public class ResourceWatcherTest
{
    [KubernetesEntity]
    public class TestResource : IKubernetesObject<V1ObjectMeta>
    {
        public string ApiVersion { get; set; } = null!;
        public string Kind { get; set; } = null!;
        public V1ObjectMeta Metadata { get; set; } = null!;
    }

    private readonly Mock<IKubernetesClient> _client = new();
    private readonly Mock<IResourceWatcherMetrics<TestResource>> _metrics = new();
    private readonly Mock<IResourceCache<TestResource>> _resourceCacheMock;

    public ResourceWatcherTest() =>
        _resourceCacheMock = new Mock<IResourceCache<TestResource>>(MockBehavior.Strict);

    [Fact]
    public async Task Should_Restart_Watcher_On_Exception()
    {
        var settings = new OperatorSettings();

        Func<Action<Exception>?> getOnError = SetupKubernetesClientWatch();

        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());
        _metrics.Setup(c => c.WatcherExceptions).Returns(Mock.Of<ICounter>());

        using var resourceWatcher = new ResourceWatcher<TestResource>(
            _client.Object,
            new NullLogger<ResourceWatcher<TestResource>>(),
            _metrics.Object,
            _resourceCacheMock.Object,
            settings);

        var testScheduler = new TestScheduler();
        resourceWatcher.TimeBasedScheduler = testScheduler;

        await resourceWatcher.StartAsync();

        getOnError()?.Invoke(new Exception());

        var backoff = settings.ErrorBackoffStrategy(1);

        testScheduler.AdvanceBy(backoff.Add(TimeSpan.FromSeconds(1)).Ticks);

        VerifyWatch(2);
    }

    private Func<Action<Exception>?> SetupKubernetesClientWatch()
    {
        Action<Exception>? onError = null;

        _client.Setup(
                c => c.Watch(
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Action<WatchEventType, TestResource>>(),
                    It.IsAny<Action<Exception>?>(),
                    It.IsAny<Action>(),
                    null,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string?>()))
            .Callback<TimeSpan, Action<WatchEventType, TestResource>, Action<Exception>?, Action?, string?, CancellationToken, string?>(
                (_, _, onErrorArg, _, _, _, _) =>
                {
                    onError = onErrorArg;
                })
            .Returns(Task.FromResult(CreateFakeWatcher()))
            .Verifiable();


        return () => onError;
    }

    [Fact]
    public async Task Should_Not_Throw_Overflow_Exception()
    {
        var settings = new OperatorSettings();

        Func<Action<Exception>?> getOnError = SetupKubernetesClientWatch();

        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());
        _metrics.Setup(c => c.WatcherExceptions).Returns(Mock.Of<ICounter>());

        using var resourceWatcher = new ResourceWatcher<TestResource>(
            _client.Object,
            new NullLogger<ResourceWatcher<TestResource>>(),
            _metrics.Object,
            _resourceCacheMock.Object,
            settings);

        var testScheduler = new TestScheduler();
        resourceWatcher.TimeBasedScheduler = testScheduler;

        await resourceWatcher.StartAsync();

        const int numberOfRetries = 40;
        for (int reconnectAttempts = 1; reconnectAttempts <= numberOfRetries; reconnectAttempts++)
        {
            getOnError()?.Invoke(new Exception());

            var backoff = settings.ErrorBackoffStrategy(reconnectAttempts > 39 ? 39 : reconnectAttempts);
            if (backoff.TotalSeconds > settings.WatcherMaxRetrySeconds)
            {
                backoff = TimeSpan.FromSeconds(settings.WatcherMaxRetrySeconds);
            }

            testScheduler.AdvanceBy(backoff.Add(TimeSpan.FromSeconds(1)).Ticks);
        }

        VerifyWatch(numberOfRetries + 1);
    }

    [Fact]
    public async Task Should_Not_Dispose_Reconnect_Subject_Or_Throw_Exception_After_Restarts()
    {
        var settings = new OperatorSettings();

        Func<Action<Exception>?> getOnError = SetupKubernetesClientWatch();

        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());
        _metrics.Setup(c => c.WatcherExceptions).Returns(Mock.Of<ICounter>());

        using var resourceWatcher = new ResourceWatcher<TestResource>(
            _client.Object, 
            new NullLogger<ResourceWatcher<TestResource>>(), 
            _metrics.Object, 
            _resourceCacheMock.Object, 
            settings);

        await resourceWatcher.StartAsync();

        await resourceWatcher.StopAsync();

        await resourceWatcher.StartAsync();

        var kubernetesException = new KubernetesException(new V1Status());

        getOnError()?.Invoke(kubernetesException);

        resourceWatcher.WatchEvents.Should().NotBeNull();

        VerifyWatch(2);
    }

    [Fact]
    public async Task Should_Not_Restart_On_Serialization_Exception()
    {
        var settings = new OperatorSettings();

        Func<Action<Exception>?> getOnError = SetupKubernetesClientWatch();

        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());
        _metrics.Setup(c => c.WatcherExceptions).Returns(Mock.Of<ICounter>());

        using var resourceWatcher = new ResourceWatcher<TestResource>(
            _client.Object, 
            new NullLogger<ResourceWatcher<TestResource>>(), 
            _metrics.Object, 
            _resourceCacheMock.Object, 
            settings);

        await resourceWatcher.StartAsync();

        var serializationException = new SerializationException(string.Empty, new JsonException("The input does not contain any JSON tokens"));

        getOnError()?.Invoke(serializationException);

        resourceWatcher.WatchEvents.Should().NotBeNull();

        VerifyWatch(1);
    }

    [Fact]
    public async Task Should_Be_Restarted_After_TaskCanceledException_IOException()
    {
        var settings = new OperatorSettings();

        Func<Action<Exception>?> getOnError = SetupKubernetesClientWatch();

        SetupResourceWatcherMetrics();

        using var resourceWatcher = new ResourceWatcher<TestResource>(
            _client.Object, 
            new NullLogger<ResourceWatcher<TestResource>>(), 
            _metrics.Object, 
            _resourceCacheMock.Object, 
            settings);

        await resourceWatcher.StartAsync();

        var serializationException = new TaskCanceledException(string.Empty, new IOException($@"Either the server or the client did close the connection on watcher for resource ""{typeof(TestResource)}"". Restart."));

        getOnError()?.Invoke(serializationException);

        VerifyWatch(2);
    }

    private void SetupResourceWatcherMetrics()
    {
        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());
        _metrics.Setup(c => c.WatcherExceptions).Returns(Mock.Of<ICounter>());
    }

    [Fact]
    public async Task Should_Publish_On_Watcher_Event()
    {
        var settings = new OperatorSettings();

        Action<WatchEventType, TestResource> onWatcherEvent = null!;

        _resourceCacheMock.Setup(c => c.Exists(It.IsAny<TestResource>()))
            .Returns(false);

        _client.Setup(
                c => c.Watch(
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Action<WatchEventType, TestResource>>(),
                    It.IsAny<Action<Exception>?>(),
                    It.IsAny<Action>(),
                    null,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string?>()))
            .Callback<TimeSpan, Action<WatchEventType, TestResource>, Action<Exception>?, Action?, string?,
                CancellationToken, string?>(
                (_, onWatcherEventArg, _, _, _, _, _) => { onWatcherEvent = onWatcherEventArg; })
            .Returns(Task.FromResult(CreateFakeWatcher()))
            .Verifiable();

        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());
        _metrics.Setup(c => c.WatchedEvents).Returns(Mock.Of<ICounter>());

        using var resourceWatcher = new ResourceWatcher<TestResource>(
            _client.Object,
            new NullLogger<ResourceWatcher<TestResource>>(),
            _metrics.Object,
            _resourceCacheMock.Object,
            settings);

        var watchEvents = resourceWatcher.WatchEvents.Replay(1);
        watchEvents.Connect();

        await resourceWatcher.StartAsync();

        var resource = new TestResource
        {
            Metadata = new()
        };

        onWatcherEvent(WatchEventType.Added, resource);

        var watchEvent = await watchEvents.FirstAsync();

        watchEvent.Type.Should().Be(WatchEventType.Added);
        watchEvent.Resource.Should().BeEquivalentTo(resource);
    }

    [Fact]
    public async Task Should_Restart_Watcher_On_Close()
    {
        var settings = new OperatorSettings();

        Action? onClose = null;

        _client.Setup(c => c.Watch(It.IsAny<TimeSpan>(), It.IsAny<Action<WatchEventType, TestResource>>(), It.IsAny<Action<Exception>?>(), It.IsAny<Action>(), null, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Callback<TimeSpan, Action<WatchEventType, TestResource>, Action<Exception>?, Action?, string?, CancellationToken, string?>(
                (_, _, _, onCloseArg, _, _, _) =>
                {
                    onClose = onCloseArg;
                })
            .Returns(Task.FromResult(CreateFakeWatcher()))
            .Verifiable();

        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());
        _metrics.Setup(c => c.WatcherClosed).Returns(Mock.Of<ICounter>());

        using var resourceWatcher = new ResourceWatcher<TestResource>(
            _client.Object, 
            new NullLogger<ResourceWatcher<TestResource>>(), 
            _metrics.Object, 
            _resourceCacheMock.Object, 
            settings);

        await resourceWatcher.StartAsync();

        onClose?.Invoke();

        VerifyWatch(2);
    }

    private void VerifyWatch(int callTime)
    {
        _client.Verify(
            c => c.Watch(
                It.IsAny<TimeSpan>(),
                It.IsAny<Action<WatchEventType, TestResource>>(),
                It.IsAny<Action<Exception>?>(),
                It.IsAny<Action>(),
                null,
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>()), Times.Exactly(callTime));
    }

    private static Watcher<TestResource> CreateFakeWatcher()
    {
        return new Watcher<TestResource>(
            () => Task.FromResult(new StreamReader(new MemoryStream())),
            (_, __) => { },
            _ => { });
    }
}