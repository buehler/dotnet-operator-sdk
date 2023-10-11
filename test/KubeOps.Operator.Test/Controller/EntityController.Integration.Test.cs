using FluentAssertions;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Test.Controller;

public class EntityControllerIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private static readonly InvocationCounter<V1IntegrationTestEntity> Mock = new();
    private IKubernetesClient<V1IntegrationTestEntity> _client = null!;

    public EntityControllerIntegrationTest(HostBuilder hostBuilder, MlcProvider provider) : base(hostBuilder, provider)
    {
        Mock.Clear();
    }

    [Fact]
    public async Task Should_Call_Reconcile_On_New_Entity()
    {
        (await _client.ListAsync("default")).Count.Should().Be(0);

        await _client.CreateAsync(new V1IntegrationTestEntity("test-entity", "username", "default"));
        await Mock.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(1);
        var (method, entity) = Mock.Invocations[0];
        method.Should().Be("ReconcileAsync");
        entity.Should().BeOfType<V1IntegrationTestEntity>();
        entity.Name().Should().Be("test-entity");
        entity.Spec.Username.Should().Be("username");
    }

    [Fact]
    public async Task Should_Call_Reconcile_On_Modification_Of_Entity()
    {
        Mock.TargetInvocationCount = 2;
        (await _client.ListAsync("default")).Count.Should().Be(0);

        var result = await _client.CreateAsync(new V1IntegrationTestEntity("test-entity", "username", "default"));
        result.Spec.Username = "changed";
        await _client.UpdateAsync(result);
        await Mock.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(2);

        Check(0, "username");
        Check(1, "changed");
        return;

        static void Check(int idx, string username)
        {
            var (method, entity) = Mock.Invocations[idx];
            method.Should().Be("ReconcileAsync");
            entity.Should().BeOfType<V1IntegrationTestEntity>();
            entity.Spec.Username.Should().Be(username);
        }
    }

    [Fact]
    public async Task Should_Call_Delete_For_Deleted_Entity()
    {
        Mock.TargetInvocationCount = 2;
        (await _client.ListAsync("default")).Count.Should().Be(0);

        var result = await _client.CreateAsync(new V1IntegrationTestEntity("test-entity", "username", "default"));
        await _client.DeleteAsync(result);
        await Mock.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(2);
        Mock.Invocations[0].Method.Should().Be("ReconcileAsync");
        Mock.Invocations[1].Method.Should().Be("DeletedAsync");
    }

    public async Task InitializeAsync()
    {
        var meta = _mlc.ToEntityMetadata(typeof(V1IntegrationTestEntity)).Metadata;
        _client = new KubernetesClient<V1IntegrationTestEntity>(meta);
        await _hostBuilder.ConfigureAndStart(builder => builder.Services
            .AddSingleton(Mock)
            .AddKubernetesOperator()
            .AddControllerWithEntity<TestController, V1IntegrationTestEntity>(meta));
    }

    public async Task DisposeAsync()
    {
        var entities = await _client.ListAsync("default");
        await _client.DeleteAsync(entities);
        _client.Dispose();
    }

    private class TestController : IEntityController<V1IntegrationTestEntity>
    {
        private readonly InvocationCounter<V1IntegrationTestEntity> _svc;

        public TestController(InvocationCounter<V1IntegrationTestEntity> svc)
        {
            _svc = svc;
        }

        public Task ReconcileAsync(V1IntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            return Task.CompletedTask;
        }

        public Task DeletedAsync(V1IntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }
}
