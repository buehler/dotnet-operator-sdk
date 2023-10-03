using FluentAssertions;

using k8s.Models;

using KubeOps.KubernetesClient;
using KubeOps.Operator.Extensions;
using KubeOps.Operator.Test.Controller.Operator;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Test.Controller;

public class EntityControllerIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private IKubernetesClient<V1IntegrationTestEntity> _client = null!;
    private static readonly ControllerMockService _mock = new();

    public EntityControllerIntegrationTest(HostBuilder hostBuilder) : base(hostBuilder)
    {
    }

    [Fact]
    public async Task Should_Call_Reconcile_On_New_Entity()
    {
        _mock.Clear();
        (await _client.List("default")).Count.Should().Be(0);

        await _client.Create(new("test-entity", "username", "default"));
        await _mock.WaitForInvocations;

        _mock.Invocations.Count.Should().Be(1);
        var (method, entity) = _mock.Invocations[0];
        method.Should().Be("Reconcile");
        entity.Should().BeOfType<V1IntegrationTestEntity>();
        entity.Name().Should().Be("test-entity");
        entity.Spec.Username.Should().Be("username");
    }

    [Fact]
    public async Task Should_Call_Reconcile_On_Modification_Of_Entity()
    {
        _mock.Clear();
        _mock.TargetInvocationCount = 2;
        (await _client.List("default")).Count.Should().Be(0);

        var result = await _client.Create(new("test-entity", "username", "default"));
        result.Spec.Username = "changed";
        await _client.Update(result);
        await _mock.WaitForInvocations;

        _mock.Invocations.Count.Should().Be(2);

        Check(0, "username");
        Check(1, "changed");
        return;

        void Check(int idx, string username)
        {
            var (method, entity) = _mock.Invocations[idx];
            method.Should().Be("Reconcile");
            entity.Should().BeOfType<V1IntegrationTestEntity>();
            entity.Spec.Username.Should().Be(username);
        }
    }
    
    [Fact]
    public async Task Should_Call_Delete_For_Deleted_Entity()
    {
        _mock.Clear();
        _mock.TargetInvocationCount = 2;
        (await _client.List("default")).Count.Should().Be(0);

        var result = await _client.Create(new("test-entity", "username", "default"));
        await _client.Delete(result);
        await _mock.WaitForInvocations;

        _mock.Invocations.Count.Should().Be(2);
        _mock.Invocations[0].Method.Should().Be("Reconcile");
        _mock.Invocations[1].Method.Should().Be("Delete");
    }

    public async Task InitializeAsync()
    {
        var meta = Entities.ToEntityMetadata(typeof(V1IntegrationTestEntity)).Metadata;
        _client = new KubernetesClient<V1IntegrationTestEntity>(meta);
        await _hostBuilder.ConfigureAndStart(builder => builder.Services
            .AddSingleton(_mock)
            .AddKubernetesOperator()
            .AddControllerWithEntity<TestController, V1IntegrationTestEntity>(meta));
    }

    public async Task DisposeAsync()
    {
        var entities = await _client.List("default");
        await _client.Delete(entities);
        _client.Dispose();
    }
}
