using FluentAssertions;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Test.Controller;

public class EntityRequeueIntegrationTest : IntegrationTestBase
{
    private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _mock = new();
    private readonly IKubernetesClient _client = new KubernetesClient.KubernetesClient();
    private readonly TestNamespaceProvider _ns = new();

    [Fact]
    public async Task Should_Not_Queue_If_Not_Requested()
    {
        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("test-entity", "username", _ns.Namespace));
        await _mock.WaitForInvocations;

        _mock.Invocations.Count.Should().Be(1);
    }

    [Fact]
    public async Task Should_Requeue_Entity_And_Reconcile()
    {
        _mock.TargetInvocationCount = 5;
        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("test-entity", "username", _ns.Namespace));
        await _mock.WaitForInvocations;

        _mock.Invocations.Count.Should().Be(5);
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

    private class TestController(InvocationCounter<V1OperatorIntegrationTestEntity> svc,
            EntityRequeue<V1OperatorIntegrationTestEntity> requeue)
        : IEntityController<V1OperatorIntegrationTestEntity>
    {
        public Task ReconcileAsync(V1OperatorIntegrationTestEntity entity)
        {
            svc.Invocation(entity);
            if (svc.Invocations.Count <= svc.TargetInvocationCount)
            {
                requeue(entity, TimeSpan.FromMilliseconds(1));
            }

            return Task.CompletedTask;
        }
    }
}
