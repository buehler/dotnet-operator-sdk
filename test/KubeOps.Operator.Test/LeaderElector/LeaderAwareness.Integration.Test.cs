using FluentAssertions;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Test.LeaderElector;

public class LeaderAwarenessIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private static readonly InvocationCounter<V1OperatorIntegrationTestEntity> Mock = new();
    private IKubernetesClient<V1OperatorIntegrationTestEntity> _client = null!;

    private readonly IKubernetesClient<V1Lease> _leaseClient = new KubernetesClient<V1Lease>(new(V1Lease.KubeKind,
        V1Lease.KubeApiVersion, V1Lease.KubeGroup, V1Lease.KubePluralName));

    public LeaderAwarenessIntegrationTest(HostBuilder hostBuilder, MlcProvider provider) : base(hostBuilder, provider)
    {
        Mock.Clear();
    }

    [Fact]
    public async Task Should_Create_V1Lease_And_Start_Watcher()
    {
        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("test-entity", "username", "default"));
        await Mock.WaitForInvocations;

        var lease = await _leaseClient.GetAsync("kubernetesoperator-leader", "default");
        lease!.Spec.HolderIdentity.Should().Be(Environment.MachineName);
    }

    public async Task InitializeAsync()
    {
        var meta = _mlc.ToEntityMetadata(typeof(V1OperatorIntegrationTestEntity)).Metadata;
        _client = new KubernetesClient<V1OperatorIntegrationTestEntity>(meta);
        await _hostBuilder.ConfigureAndStart(builder => builder.Services
            .AddSingleton(Mock)
            .AddKubernetesOperator(s => s.EnableLeaderElection = true)
            .AddController<TestController, V1OperatorIntegrationTestEntity>());
    }

    public async Task DisposeAsync()
    {
        var entities = await _client.ListAsync("default");
        await _client.DeleteAsync(entities);
        await _leaseClient.DeleteAsync(await _leaseClient.ListAsync("default"));
        _client.Dispose();
    }

    private class TestController : IEntityController<V1OperatorIntegrationTestEntity>
    {
        private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _svc;

        public TestController(InvocationCounter<V1OperatorIntegrationTestEntity> svc)
        {
            _svc = svc;
        }

        public Task ReconcileAsync(V1OperatorIntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            return Task.CompletedTask;
        }

        public Task DeletedAsync(V1OperatorIntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }
}
