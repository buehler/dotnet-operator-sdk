using FluentAssertions;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Test.Controller;

public class EntityRequeueIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private static readonly InvocationCounter<V1OperatorIntegrationTestEntity> Mock = new();
    private IKubernetesClient<V1OperatorIntegrationTestEntity> _client = null!;

    public EntityRequeueIntegrationTest(HostBuilder hostBuilder, MlcProvider provider) : base(hostBuilder, provider)
    {
        Mock.Clear();
    }

    [Fact]
    public async Task Should_Not_Queue_If_Not_Requested()
    {
        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("test-entity", "username", "default"));
        await Mock.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(1);
    }

    [Fact]
    public async Task Should_Requeue_Entity_And_Reconcile()
    {
        Mock.TargetInvocationCount = 5;
        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("test-entity", "username", "default"));
        await Mock.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(5);
    }

    public async Task InitializeAsync()
    {
        var meta = _mlc.ToEntityMetadata(typeof(V1OperatorIntegrationTestEntity)).Metadata;
        _client = new KubernetesClient<V1OperatorIntegrationTestEntity>(meta);
        await _hostBuilder.ConfigureAndStart(builder => builder.Services
            .AddSingleton(Mock)
            .AddKubernetesOperator()
            .AddController<TestController, V1OperatorIntegrationTestEntity>());
    }

    public async Task DisposeAsync()
    {
        var entities = await _client.ListAsync("default");
        await _client.DeleteAsync(entities);
        _client.Dispose();
    }

    private class TestController : IEntityController<V1OperatorIntegrationTestEntity>
    {
        private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _svc;
        private readonly EntityRequeue<V1OperatorIntegrationTestEntity> _requeue;

        public TestController(
            InvocationCounter<V1OperatorIntegrationTestEntity> svc,
            EntityRequeue<V1OperatorIntegrationTestEntity> requeue)
        {
            _svc = svc;
            _requeue = requeue;
        }

        public Task ReconcileAsync(V1OperatorIntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            if (_svc.Invocations.Count <= _svc.TargetInvocationCount)
            {
                _requeue(entity, TimeSpan.FromMilliseconds(1));
            }

            return Task.CompletedTask;
        }
    }
}
