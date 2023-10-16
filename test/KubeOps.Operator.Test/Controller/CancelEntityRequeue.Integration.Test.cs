using FluentAssertions;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Test.Controller;

public class CancelEntityRequeueIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private static readonly InvocationCounter<V1OperatorIntegrationTestEntity> Mock = new();
    private IKubernetesClient<V1OperatorIntegrationTestEntity> _client = null!;

    public CancelEntityRequeueIntegrationTest(HostBuilder hostBuilder, MlcProvider provider)
        : base(hostBuilder, provider)
    {
        Mock.Clear();
    }

    [Fact]
    public async Task Should_Cancel_Requeue_If_New_Event_Fires()
    {
        // This test fires the reconcile, which in turn requeues the entity.
        // then immediately fires a new event, which should cancel the requeue.

        Mock.TargetInvocationCount = 2;
        var e = await _client.CreateAsync(new V1OperatorIntegrationTestEntity("test-entity", "username", "default"));
        e.Spec.Username = "changed";
        await _client.UpdateAsync(e);
        await Mock.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(2);
        _hostBuilder.Services.GetRequiredService<TimedEntityQueue<V1OperatorIntegrationTestEntity>>().Count.Should().Be(0);
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
            if (_svc.Invocations.Count < 2)
            {
                _requeue(entity, TimeSpan.FromMilliseconds(1000));
            }

            return Task.CompletedTask;
        }
    }
}
