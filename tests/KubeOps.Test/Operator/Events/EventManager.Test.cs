using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator;
using KubeOps.Operator.Events;
using Microsoft.Extensions.Logging;
using Moq;
using SimpleBase;
using Xunit;

namespace KubeOps.Test.Operator.Events;

public class EventManagerTest
{
    private readonly Mock<IKubernetesClient> _mockedClient = new();
    private readonly IEventManager _manager;

    public EventManagerTest()
    {
        _manager = new EventManager(
            _mockedClient.Object,
            new OperatorSettings { Name = "test-operator" },
            new Mock<ILogger<EventManager>>(MockBehavior.Loose).Object);
    }

    [Fact]
    public async Task Should_Publish_Event_Directly_On_ApiClient()
    {
        _mockedClient.Setup(client => client.Save(It.IsAny<Corev1Event>()));
        _mockedClient.Verify(client => client.Save(It.IsAny<Corev1Event>()), Times.Never);

        await _manager.PublishAsync(new Corev1Event());

        _mockedClient.Verify(client => client.Save(It.IsAny<Corev1Event>()), Times.Once);
    }

    [Fact]
    public async Task Should_Publish_With_New_Event()
    {
        var (eventName, testResource) = GetTestEvent();

        _mockedClient
            .Setup(client => client.Get<Corev1Event>(eventName, testResource.Namespace()))
            .Returns(Task.FromResult<Corev1Event?>(null));
        _mockedClient.Setup(client => client.Save(It.IsAny<Corev1Event>()));
        _mockedClient.Verify(client => client.Save(It.IsAny<Corev1Event>()), Times.Never);

        await _manager.PublishAsync(testResource, "REASON", "MESSAGE");

        _mockedClient.Verify(client => client.Save(It.IsAny<Corev1Event>()), Times.Once);
        _mockedClient.Verify(client => client.Get<Corev1Event>(eventName, testResource.Namespace()), Times.Once);
    }

    [Fact]
    public async Task Should_Publish_With_Existing_Event()
    {
        var (eventName, testResource) = GetTestEvent();

        _mockedClient
            .Setup(client => client.Get<Corev1Event>(eventName, testResource.Namespace()))
            .Returns(
                Task.FromResult<Corev1Event?>(
                    new Corev1Event
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Uid = "1234", Name = eventName, NamespaceProperty = testResource.Namespace(),
                        },
                        Count = 1,
                        LastTimestamp = DateTime.MinValue,
                    }));
        _mockedClient.Setup(client => client.Save(It.IsAny<Corev1Event>()));
        _mockedClient.Verify(client => client.Save(It.IsAny<Corev1Event>()), Times.Never);

        await _manager.PublishAsync(testResource, "REASON", "MESSAGE");

        _mockedClient.Verify(client => client.Get<Corev1Event>(eventName, testResource.Namespace()), Times.Once);
        _mockedClient.Verify(
            client => client.Save(
                It.Is<Corev1Event>(
                    ev => ev.Name() == eventName && ev.Uid() == "1234")),
            Times.Once);
    }

    [Fact]
    public async Task Should_Update_Event_Count_On_Existing_Event()
    {
        var (eventName, testResource) = GetTestEvent();

        _mockedClient
            .Setup(client => client.Get<Corev1Event>(eventName, testResource.Namespace()))
            .Returns(
                Task.FromResult<Corev1Event?>(
                    new Corev1Event
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Uid = "1234", Name = eventName, NamespaceProperty = testResource.Namespace(),
                        },
                        Count = 4,
                        LastTimestamp = DateTime.MinValue,
                    }));
        _mockedClient.Setup(client => client.Save(It.IsAny<Corev1Event>()));
        _mockedClient.Verify(client => client.Save(It.IsAny<Corev1Event>()), Times.Never);

        await _manager.PublishAsync(testResource, "REASON", "MESSAGE");

        _mockedClient.Verify(client => client.Get<Corev1Event>(eventName, testResource.Namespace()), Times.Once);
        _mockedClient.Verify(
            client => client.Save(
                It.Is<Corev1Event>(
                    ev => ev.Uid() == "1234" && ev.Count == 5)),
            Times.Once);
    }

    [Fact]
    public async Task Should_Set_Event_Count_On_New_Event()
    {
        var (eventName, testResource) = GetTestEvent();

        _mockedClient
            .Setup(client => client.Get<Corev1Event>(eventName, testResource.Namespace()))
            .Returns(Task.FromResult<Corev1Event?>(null));
        _mockedClient.Setup(client => client.Save(It.IsAny<Corev1Event>()));
        _mockedClient.Verify(client => client.Save(It.IsAny<Corev1Event>()), Times.Never);

        await _manager.PublishAsync(testResource, "REASON", "MESSAGE");

        _mockedClient.Verify(client => client.Get<Corev1Event>(eventName, testResource.Namespace()), Times.Once);
        _mockedClient.Verify(
            client => client.Save(
                It.Is<Corev1Event>(
                    ev => ev.Uid() == null && ev.Count == 1)),
            Times.Once);
    }

    [Fact]
    public void Should_Return_StaticPublisher()
    {
        var (_, testResource) = GetTestEvent();
        var pub = _manager.CreatePublisher(testResource, "REASON", "MESSAGE");

        pub.Should().BeOfType<IEventManager.AsyncStaticPublisher>();
    }

    [Fact]
    public void Should_Return_Publisher()
    {
        var pub = _manager.CreatePublisher("REASON", "MESSAGE");

        pub.Should().BeOfType<IEventManager.AsyncPublisher>();
    }

    [Fact]
    public async Task Should_Publish_With_StaticPublisher()
    {
        var (eventName, testResource) = GetTestEvent();
        _mockedClient
            .Setup(client => client.Get<Corev1Event>(eventName, testResource.Namespace()))
            .Returns(Task.FromResult<Corev1Event?>(null));
        _mockedClient.Setup(client => client.Save(It.IsAny<Corev1Event>()));

        var pub = _manager.CreatePublisher(testResource, "REASON", "MESSAGE");
        await pub();

        _mockedClient.Verify(client => client.Save(It.Is<Corev1Event>(ev => ev.Name() == eventName)), Times.Once);
    }

    [Fact]
    public async Task Should_Publish_With_Publisher()
    {
        var (eventName, testResource) = GetTestEvent();
        _mockedClient
            .Setup(client => client.Get<Corev1Event>(eventName, testResource.Namespace()))
            .Returns(Task.FromResult<Corev1Event?>(null));
        _mockedClient.Setup(client => client.Save(It.IsAny<Corev1Event>()));

        var pub = _manager.CreatePublisher("REASON", "MESSAGE");
        await pub(testResource);

        _mockedClient.Verify(client => client.Save(It.Is<Corev1Event>(ev => ev.Name() == eventName)), Times.Once);
    }

    private static (string EventName, IKubernetesObject<V1ObjectMeta> TestResource) GetTestEvent()
    {
        var resource = new TestResource();
        var name = Base32.Rfc4648.Encode(
            SHA512.HashData(
                Encoding.UTF8.GetBytes(
                    $"{resource.Name()}.{resource.Namespace()}.REASON.MESSAGE.{EventType.Normal}")));

        return (name, resource);
    }

    private class TestResource : IKubernetesObject<V1ObjectMeta>
    {
        public string ApiVersion { get; set; } = "testing.dev/v1";

        public string Kind { get; set; } = "test";

        public V1ObjectMeta Metadata { get; set; } =
            new() { Name = "test-resource", NamespaceProperty = "test-ns" };
    }
}
