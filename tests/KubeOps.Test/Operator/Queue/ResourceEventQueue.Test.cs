using System;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using FluentAssertions;
using k8s;
using k8s.Models;
using KubeOps.Operator;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Watcher;
using KubeOps.Test.TestEntities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KubeOps.Test.Operator.Queue
{
    public class ResourceEventQueueTest : IDisposable
    {
        private readonly IResourceEventQueue<TestStatusEntity> _queue;
        private readonly Mock<IKubernetesClient> _client = new Mock<IKubernetesClient>();

        private readonly Mock<ILogger<ResourceEventQueue<TestStatusEntity>>> _logger =
            new Mock<ILogger<ResourceEventQueue<TestStatusEntity>>>();

        private readonly Mock<IResourceCache<TestStatusEntity>> _cache = new Mock<IResourceCache<TestStatusEntity>>();

        private readonly Mock<IResourceWatcher<TestStatusEntity>> _watcher =
            new Mock<IResourceWatcher<TestStatusEntity>>();

        private delegate void CacheResult(TestStatusEntity resource, out CacheComparisonResult result);

        public ResourceEventQueueTest()
        {
            _queue = new ResourceEventQueue<TestStatusEntity>(
                _logger.Object,
                _client.Object,
                _cache.Object,
                _watcher.Object,
                new OperatorSettings());
        }

        public void Dispose()
        {
            _queue.Dispose();
        }

        [Fact]
        public async Task Should_Start()
        {
            await _queue.Start();
        }

        [Fact]
        public async Task Should_Fire_A_Created_Event()
        {
            var called = false;
            _cache
                .Setup(c => c.Upsert(It.IsAny<TestStatusEntity>(), out It.Ref<CacheComparisonResult>.IsAny))
                .Callback(
                    new CacheResult(
                        (TestStatusEntity resource, out CacheComparisonResult result) =>
                        {
                            result = CacheComparisonResult.New;
                        }))
                .Returns<TestStatusEntity, CacheComparisonResult>((entity, result) => entity);
            _queue.ResourceEvent += (_, tuple) =>
            {
                var (type, resource) = tuple;
                type.Should().Be(ResourceEventType.Created);
                called = true;
            };
            await _queue.Start();
            _watcher.Raise(
                w => w.WatcherEvent += null,
                null,
                (WatchEventType.Added, new TestStatusEntity { Metadata = new V1ObjectMeta(uid: "uuid") }));
            await Task.Yield();
            await Task.Delay(50);
            called.Should().BeTrue();
        }

        [Fact]
        public async Task Should_Fire_A_Updated_Event()
        {
            var called = false;
            _cache
                .Setup(c => c.Upsert(It.IsAny<TestStatusEntity>(), out It.Ref<CacheComparisonResult>.IsAny))
                .Callback(
                    new CacheResult(
                        (TestStatusEntity resource, out CacheComparisonResult result) =>
                        {
                            result = CacheComparisonResult.Modified;
                        }))
                .Returns<TestStatusEntity, CacheComparisonResult>((entity, result) => entity);
            _queue.ResourceEvent += (_, tuple) =>
            {
                var (type, resource) = tuple;
                type.Should().Be(ResourceEventType.Updated);
                called = true;
            };
            await _queue.Start();
            _watcher.Raise(
                w => w.WatcherEvent += null,
                null,
                (WatchEventType.Added, new TestStatusEntity { Metadata = new V1ObjectMeta(uid: "uuid") }));
            await Task.Yield();
            await Task.Delay(50);
            called.Should().BeTrue();
        }

        [Fact]
        public async Task Should_Fire_A_StatusUpdated_Event()
        {
            var called = false;
            _cache
                .Setup(c => c.Upsert(It.IsAny<TestStatusEntity>(), out It.Ref<CacheComparisonResult>.IsAny))
                .Callback(
                    new CacheResult(
                        (TestStatusEntity resource, out CacheComparisonResult result) =>
                        {
                            result = CacheComparisonResult.StatusModified;
                        }))
                .Returns<TestStatusEntity, CacheComparisonResult>((entity, result) => entity);
            _queue.ResourceEvent += (_, tuple) =>
            {
                var (type, resource) = tuple;
                type.Should().Be(ResourceEventType.StatusUpdated);
                called = true;
            };
            await _queue.Start();
            _watcher.Raise(
                w => w.WatcherEvent += null,
                null,
                (WatchEventType.Added, new TestStatusEntity { Metadata = new V1ObjectMeta(uid: "uuid") }));
            await Task.Yield();
            await Task.Delay(50);
            called.Should().BeTrue();
        }

        [Fact]
        public async Task Should_Fire_A_NotModified_Event()
        {
            var called = false;
            _cache
                .Setup(c => c.Upsert(It.IsAny<TestStatusEntity>(), out It.Ref<CacheComparisonResult>.IsAny))
                .Callback(
                    new CacheResult(
                        (TestStatusEntity resource, out CacheComparisonResult result) =>
                        {
                            result = CacheComparisonResult.NotModified;
                        }))
                .Returns<TestStatusEntity, CacheComparisonResult>((entity, result) => entity);
            _queue.ResourceEvent += (_, tuple) =>
            {
                var (type, resource) = tuple;
                type.Should().Be(ResourceEventType.NotModified);
                called = true;
            };
            await _queue.Start();
            _watcher.Raise(
                w => w.WatcherEvent += null,
                null,
                (WatchEventType.Added, new TestStatusEntity { Metadata = new V1ObjectMeta(uid: "uuid") }));
            await Task.Yield();
            await Task.Delay(50);
            called.Should().BeTrue();
        }

        [Theory]
        [InlineData(CacheComparisonResult.New)]
        [InlineData(CacheComparisonResult.Modified)]
        [InlineData(CacheComparisonResult.NotModified)]
        public async Task Should_Fire_A_Finalizing_Event(CacheComparisonResult cacheResult)
        {
            var called = false;
            _cache
                .Setup(c => c.Upsert(It.IsAny<TestStatusEntity>(), out It.Ref<CacheComparisonResult>.IsAny))
                .Callback(
                    new CacheResult(
                        (TestStatusEntity resource, out CacheComparisonResult result) => { result = cacheResult; }))
                .Returns<TestStatusEntity, CacheComparisonResult>((entity, result) => entity);
            _queue.ResourceEvent += (_, tuple) =>
            {
                var (type, resource) = tuple;
                type.Should().Be(ResourceEventType.Finalizing);
                called = true;
            };
            await _queue.Start();
            _watcher.Raise(
                w => w.WatcherEvent += null,
                null,
                (WatchEventType.Added,
                    new TestStatusEntity
                        { Metadata = new V1ObjectMeta(uid: "uuid", deletionTimestamp: DateTime.UtcNow) }));
            await Task.Yield();
            await Task.Delay(50);
            called.Should().BeTrue();
        }

        [Fact]
        public async Task Should_Fire_A_Deleted_Event()
        {
            var called = false;
            _queue.ResourceEvent += (_, tuple) =>
            {
                var (type, resource) = tuple;
                type.Should().Be(ResourceEventType.Deleted);
                called = true;
            };
            await _queue.Start();
            _watcher.Raise(
                w => w.WatcherEvent += null,
                null,
                (WatchEventType.Deleted, new TestStatusEntity { Metadata = new V1ObjectMeta(uid: "uuid") }));
            await Task.Yield();
            await Task.Delay(50);
            called.Should().BeTrue();
        }

        [Fact]
        public async Task Should_Fire_A_Delayed_Event()
        {
            var called = false;
            _cache
                .Setup(c => c.Upsert(It.IsAny<TestStatusEntity>(), out It.Ref<CacheComparisonResult>.IsAny))
                .Callback(
                    new CacheResult(
                        (TestStatusEntity resource, out CacheComparisonResult result) =>
                        {
                            result = CacheComparisonResult.NotModified;
                        }))
                .Returns<TestStatusEntity, CacheComparisonResult>((entity, result) => entity);
            _client
                .Setup(c => c.Get<TestStatusEntity>(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(new TestStatusEntity { Metadata = new V1ObjectMeta(uid: "uuid") });
            _queue.ResourceEvent += (_, tuple) =>
            {
                var (type, resource) = tuple;
                type.Should().Be(ResourceEventType.NotModified);
                called = true;
            };
            await _queue.Start();
            await _queue.Enqueue(
                new TestStatusEntity { Metadata = new V1ObjectMeta(uid: "uuid") },
                TimeSpan.FromMilliseconds(2));
            await Task.Yield();
            await Task.Delay(50);
            called.Should().BeTrue();
        }
    }
}
