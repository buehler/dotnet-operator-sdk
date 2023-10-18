using System.Security.Cryptography;
using System.Text;

using FluentAssertions;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Events;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Test.Events;

public class EventPublisherIntegrationTest : IntegrationTestBase
{
    private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _mock = new();
    private readonly IKubernetesClient _client = new KubernetesClient.KubernetesClient();
    private readonly TestNamespaceProvider _ns = new();

    [Fact]
    public async Task Should_Create_New_Event()
    {
        var eventName = $"single-entity.{_ns.Namespace}.REASON.message.Normal";
        var encodedEventName =
            Convert.ToHexString(
                SHA512.HashData(
                    Encoding.UTF8.GetBytes(eventName)));

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("single-entity", "username", _ns.Namespace));
        await _mock.WaitForInvocations;

        var e = await _client.GetAsync<Corev1Event>(encodedEventName, _ns.Namespace);
        e!.Count.Should().Be(1);
        e.Metadata.Annotations.Should().Contain(a => a.Key == "originalName" && a.Value == eventName);
    }

    [Fact]
    public async Task Should_Increase_Count_On_Existing_Event()
    {
        _mock.TargetInvocationCount = 5;
        var eventName = $"test-entity.{_ns.Namespace}.REASON.message.Normal";
        var encodedEventName =
            Convert.ToHexString(
                SHA512.HashData(
                    Encoding.UTF8.GetBytes(eventName)));

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("test-entity", "username", _ns.Namespace));
        await _mock.WaitForInvocations;

        var e = await _client.GetAsync<Corev1Event>(encodedEventName, _ns.Namespace);
        e!.Count.Should().Be(5);
        e.Metadata.Annotations.Should().Contain(a => a.Key == "originalName" && a.Value == eventName);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await _ns.InitializeAsync();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _ns.DisposeAsync();
        _client.Dispose();
    }

    protected override void ConfigureHost(HostApplicationBuilder builder)
    {
        builder.Services
            .AddSingleton(_mock)
            .AddKubernetesOperator(s => s.Namespace = _ns.Namespace)
            .AddController<TestController, V1OperatorIntegrationTestEntity>();
    }

    private class TestController : IEntityController<V1OperatorIntegrationTestEntity>
    {
        private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _svc;
        private readonly EntityRequeue<V1OperatorIntegrationTestEntity> _requeue;
        private readonly EventPublisher _eventPublisher;

        public TestController(
            InvocationCounter<V1OperatorIntegrationTestEntity> svc,
            EntityRequeue<V1OperatorIntegrationTestEntity> requeue,
            EventPublisher eventPublisher)
        {
            _svc = svc;
            _requeue = requeue;
            _eventPublisher = eventPublisher;
        }

        public async Task ReconcileAsync(V1OperatorIntegrationTestEntity entity)
        {
            await _eventPublisher(entity, "REASON", "message");
            _svc.Invocation(entity);

            if (_svc.Invocations.Count < _svc.TargetInvocationCount)
            {
                _requeue(entity, TimeSpan.FromMilliseconds(10));
            }
        }
    }
}
