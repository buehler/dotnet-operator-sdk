using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using k8s;
using k8s.Models;
using KubeOps.Operator;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Kubernetes;
using KubeOps.Testing;
using KubeOps.TestOperator.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KubeOps.Test.Operator.Controller;

public class EventQueueTest
{
    private readonly Subject<ResourceWatcher<V1TestEntity>.WatchEvent> _watcherEventSource;
    private readonly MockKubernetesClient _kubernetesClient;

    private readonly Mock<IResourceCache<V1TestEntity>> _resourceCacheMock;
    private readonly Mock<IResourceWatcher<V1TestEntity>> _resourceWatcherMock;

    private readonly IEventQueue<V1TestEntity> _eventQueue;

    public EventQueueTest()
    {
        _watcherEventSource = new Subject<ResourceWatcher<V1TestEntity>.WatchEvent>();

        _kubernetesClient = new MockKubernetesClient();
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<EventQueue<V1TestEntity>>();
        var operatorSettings = new OperatorSettings { MaxErrorRetries = 3, };

        var cacheComparisonResult = CacheComparisonResult.Other;

        _resourceCacheMock = new Mock<IResourceCache<V1TestEntity>>(MockBehavior.Strict);
        _resourceCacheMock.Setup(c => c.Upsert(It.IsAny<V1TestEntity>(), out cacheComparisonResult))
            .Returns(new Func<V1TestEntity, CacheComparisonResult, V1TestEntity>((e, _) => e));
        _resourceCacheMock.Setup(c => c.Remove(It.IsAny<V1TestEntity>()));

        _resourceWatcherMock = new Mock<IResourceWatcher<V1TestEntity>>(MockBehavior.Strict);
        _resourceWatcherMock.SetupGet(w => w.WatchEvents).Returns(_watcherEventSource);
        _resourceWatcherMock.Setup(w => w.StartAsync()).Returns(Task.CompletedTask);
        _resourceWatcherMock.Setup(w => w.StopAsync()).Returns(Task.CompletedTask);

        _eventQueue = new EventQueue<V1TestEntity>(
            _kubernetesClient,
            logger,
            operatorSettings,
            _resourceCacheMock.Object,
            _resourceWatcherMock.Object);
    }

    private V1TestEntity EntityWithUid(int id) => new() { Metadata = new V1ObjectMeta { Uid = id.ToString() }, };

    [Fact]
    public async Task Local_Events_Should_Be_Processed()
    {
        var numberEventsProcessed = 0;

        var _ = _eventQueue.Events.Do(_ => { numberEventsProcessed++; }).Subscribe();

        await _eventQueue.StartAsync(_ => { });

        _eventQueue.EnqueueLocal(new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, EntityWithUid(1)));
        _eventQueue.EnqueueLocal(new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, EntityWithUid(2)));
        _eventQueue.EnqueueLocal(new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, EntityWithUid(3)));
        await Task.Delay(100);

        numberEventsProcessed.Should().Be(3);
    }

    [Fact]
    public async Task Watcher_Events_Should_Be_Processed()
    {
        var numberEventsProcessed = 0;

        var _ = _eventQueue.Events.Do(_ => { numberEventsProcessed++; }).Subscribe();

        await _eventQueue.StartAsync(_ => { });

        _watcherEventSource.OnNext(
            new ResourceWatcher<V1TestEntity>.WatchEvent(WatchEventType.Deleted, EntityWithUid(1)));
        _watcherEventSource.OnNext(
            new ResourceWatcher<V1TestEntity>.WatchEvent(WatchEventType.Deleted, EntityWithUid(2)));
        _watcherEventSource.OnNext(
            new ResourceWatcher<V1TestEntity>.WatchEvent(WatchEventType.Deleted, EntityWithUid(3)));
        await Task.Delay(100);

        numberEventsProcessed.Should().Be(3);
    }

    [Fact]
    public async Task Duplicate_Events_Should_Be_Clobbered()
    {
        var numberEventsProcessed = 0;

        var _ = _eventQueue.Events.Do(_ => { numberEventsProcessed++; }).Subscribe();

        await _eventQueue.StartAsync(_ => { });

        _eventQueue.EnqueueLocal(new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, EntityWithUid(1)));
        _eventQueue.EnqueueLocal(
            new ResourceEvent<V1TestEntity>(
                ResourceEventType.Reconcile,
                EntityWithUid(2),
                Delay: TimeSpan.FromMilliseconds(50)));
        _eventQueue.EnqueueLocal(new ResourceEvent<V1TestEntity>(ResourceEventType.Deleted, EntityWithUid(2)));
        await Task.Delay(100);

        numberEventsProcessed.Should().Be(2);
    }

    [Fact]
    public async Task Events_With_Too_Many_Retries_Should_Be_Dropped()
    {
        var numberEventsProcessed = 0;

        var _ = _eventQueue.Events.Do(_ => { numberEventsProcessed++; }).Subscribe();

        await _eventQueue.StartAsync(_ => { });

        _eventQueue.EnqueueLocal(new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, EntityWithUid(1)));
        _eventQueue.EnqueueLocal(
            new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, EntityWithUid(2), Attempt: 2));
        _eventQueue.EnqueueLocal(
            new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, EntityWithUid(3), Attempt: 4));
        await Task.Delay(100);

        numberEventsProcessed.Should().Be(2);
    }

    [Fact]
    public async Task Delayed_Events_Are_Handled_After_Delay()
    {
        var entity = EntityWithUid(1);
        _kubernetesClient.GetResult = entity;

        var numberEventsProcessed = 0;

        var _ = _eventQueue.Events.Do(_ => { numberEventsProcessed++; }).Subscribe();

        await _eventQueue.StartAsync(_ => { });

        _eventQueue.EnqueueLocal(
            new ResourceEvent<V1TestEntity>(ResourceEventType.Reconcile, entity, Delay: TimeSpan.FromSeconds(4)));

        await Task.Delay(3500);
        numberEventsProcessed.Should().Be(0);

        await Task.Delay(4500);
        numberEventsProcessed.Should().Be(1);
    }

    [Fact]
    public async Task Delayed_Events_With_Missing_Entities_Are_Dropped()
    {
        var numberEventsProcessed = 0;
        _kubernetesClient.GetResult = null;

        var _ = _eventQueue.Events.Do(_ => { numberEventsProcessed++; }).Subscribe();

        await _eventQueue.StartAsync(_ => { });

        _eventQueue.EnqueueLocal(
            new ResourceEvent<V1TestEntity>(
                ResourceEventType.Reconcile,
                EntityWithUid(1),
                Delay: TimeSpan.FromMilliseconds(1)));

        await Task.Delay(100);

        numberEventsProcessed.Should().Be(0);
    }
}
