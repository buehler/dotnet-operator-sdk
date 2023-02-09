using FluentAssertions;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Kubernetes;
using KubeOps.Testing;
using Microsoft.Extensions.Logging.Abstractions;
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

    private readonly IKubernetesClient _client = new MockKubernetesClient();
    private readonly Mock<IResourceWatcherMetrics<TestResource>> _metrics = new();

    [Fact]
    public async Task Should_Not_Dispose_Reconnect_Subject_Or_Throw_Exception_After_Restarts()
    {
        var settings = new OperatorSettings();

        _metrics.Setup(c => c.Running).Returns(Mock.Of<IGauge>());

        using var resourceWatcher = new ResourceWatcher<TestResource>(_client, new NullLogger<ResourceWatcher<TestResource>>(), _metrics.Object, settings);

        await resourceWatcher.StartAsync();

        await resourceWatcher.StopAsync();

        await resourceWatcher.StartAsync();

        resourceWatcher.WatchEvents.Should().NotBeNull();
    }
}
