using FluentAssertions;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Test;

public class NamespacedOperatorIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private static readonly InvocationCounter<V1IntegrationTestEntity> Mock = new();
    private IKubernetesClient<V1IntegrationTestEntity> _client = null!;
    private IKubernetesClient<V1Namespace> _nsClient = null!;

    public NamespacedOperatorIntegrationTest(HostBuilder hostBuilder, MlcProvider provider) : base(hostBuilder, provider)
    {
        Mock.Clear();
    }

    [Fact]
    public async Task Should_Call_Reconcile_On_Entity_In_Namespace()
    {
        var watcherCounter = new InvocationCounter<V1IntegrationTestEntity> { TargetInvocationCount = 2 };
        using var watcher = _client.Watch((_, e) => watcherCounter.Invocation(e));

        await _client.CreateAsync(new V1IntegrationTestEntity("test-entity", "username", "foobar"));
        await _client.CreateAsync(new V1IntegrationTestEntity("test-entity", "username", "default"));
        await Mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;
        Mock.Invocations.Count.Should().Be(1);
        watcherCounter.Invocations.Count.Should().Be(2);
    }

    [Fact]
    public async Task Should_Not_Call_Reconcile_On_Entity_In_Other_Namespace()
    {
        var watcherCounter = new InvocationCounter<V1IntegrationTestEntity> { TargetInvocationCount = 1 };
        using var watcher = _client.Watch((_, e) => watcherCounter.Invocation(e));

        await _client.CreateAsync(new V1IntegrationTestEntity("test-entity2", "username", "default"));
        await watcherCounter.WaitForInvocations;
        Mock.Invocations.Count.Should().Be(0);
        watcherCounter.Invocations.Count.Should().Be(1);
    }

    public async Task InitializeAsync()
    {
        var meta = _mlc.ToEntityMetadata(typeof(V1IntegrationTestEntity)).Metadata;
        _client = new KubernetesClient<V1IntegrationTestEntity>(meta);
        _nsClient = new KubernetesClient<V1Namespace>(new(V1Namespace.KubeKind, V1Namespace.KubeApiVersion,
            V1Namespace.KubeGroup, V1Namespace.KubePluralName));
        await _nsClient.SaveAsync(new V1Namespace(metadata: new(name: "foobar")).Initialize());
        await _hostBuilder.ConfigureAndStart(builder => builder.Services
            .AddSingleton(Mock)
            .AddKubernetesOperator(s => s.Namespace = "foobar")
            .AddController<TestController, V1IntegrationTestEntity>());
    }

    public async Task DisposeAsync()
    {
        var entities = await _client.ListAsync("default");
        await _nsClient.DeleteAsync("foobar");
        while (await _nsClient.GetAsync("foobar") is not null)
        {
            await Task.Delay(100);
        }
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
