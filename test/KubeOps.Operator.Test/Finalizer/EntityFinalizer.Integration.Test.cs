using FluentAssertions;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Test.Finalizer;

public class EntityFinalizerIntegrationTest : IntegrationTestBase, IAsyncLifetime
{
    private static readonly InvocationCounter<V1OperatorIntegrationTestEntity> Mock = new();
    private IKubernetesClient<V1OperatorIntegrationTestEntity> _client = null!;

    public EntityFinalizerIntegrationTest(HostBuilder hostBuilder, MlcProvider provider) : base(hostBuilder, provider)
    {
        Mock.Clear();
    }

    [Fact]
    public async Task Should_Not_Call_Controller_When_Attaching_Finalizer()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 2 };
        using var watcher = _client.Watch((_, e) => watcherCounter.Invocation(e));

        (await _client.ListAsync("default")).Count.Should().Be(0);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first", "first", "default"));
        await Mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(1);
        // the resource watcher will be called twice (since the finalizer is added).
        watcherCounter.Invocations.Count.Should().Be(2);
        var (method, entity) = Mock.Invocations[0];
        method.Should().Be("ReconcileAsync");
        entity.Name().Should().Be("first");
    }

    [Fact]
    public async Task Should_Not_Call_Controller_When_Attaching_Finalizers()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using var watcher = _client.Watch((_, e) => watcherCounter.Invocation(e));

        (await _client.ListAsync("default")).Count.Should().Be(0);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first-second", "first-second", "default"));
        await Mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        Mock.Invocations.Count.Should().Be(1);
        // the resource watcher will be called trice (since the finalizers are added).
        watcherCounter.Invocations.Count.Should().Be(3);
        var (method, entity) = Mock.Invocations[0];
        method.Should().Be("ReconcileAsync");
        entity.Name().Should().Be("first-second");
    }

    [Fact]
    public async Task Should_Attach_Finalizer_On_Entity()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 2 };
        using var watcher = _client.Watch((_, e) => watcherCounter.Invocation(e));

        (await _client.ListAsync("default")).Count.Should().Be(0);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first", "first", "default"));
        await Mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        var result = await _client.GetAsync("first", "default");
        result!.Metadata.Finalizers.Should().Contain("first");
    }

    [Fact]
    public async Task Should_Attach_Multiple_Finalizer_On_Entity()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using var watcher = _client.Watch((_, e) => watcherCounter.Invocation(e));

        (await _client.ListAsync("default")).Count.Should().Be(0);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first-second", "first-second", "default"));
        await Mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        var result = await _client.GetAsync("first-second", "default");
        result!.Metadata.Finalizers.Should().Contain("first");
        result.Metadata.Finalizers.Should().Contain("second");
    }

    [Fact]
    public async Task Should_Finalize_And_Delete_An_Entity()
    {
        var attachCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 2 };
        using (_client.Watch((_, e) => attachCounter.Invocation(e)))
        {
            (await _client.ListAsync("default")).Count.Should().Be(0);

            await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first", "first", "default"));

            // 1 invocation for create
            await Mock.WaitForInvocations;
            // 2 invocations: create, add finalizer
            await attachCounter.WaitForInvocations;
        }

        var oldInvocs = Mock.Invocations.ToList();

        // reset to catch 1 invocation: deleted
        Mock.Clear();

        // 3 invocations: 1 when the watcher is created and the entity already exists, 1 for finalize, 1 for delete.
        var finalizeCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using (_client.Watch((_, e) => finalizeCounter.Invocation(e)))
        {
            await _client.DeleteAsync("first", "default");
            // 2 invocations: call to delete, update after finalize
            await finalizeCounter.WaitForInvocations;
            // 1 invocation for delete
            await Mock.WaitForInvocations;
        }

        oldInvocs.Should().Contain(i => i.Method == "ReconcileAsync");
        Mock.Invocations.Should().Contain(i => i.Method == "DeletedAsync");
        Mock.Invocations.Should().Contain(i => i.Method == "FinalizeAsync");
    }

    [Fact]
    public async Task Should_Finalize_And_Delete_An_Entity_With_Multiple_Finalizer()
    {
        var attachCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using (_client.Watch((_, e) => attachCounter.Invocation(e)))
        {
            (await _client.ListAsync("default")).Count.Should().Be(0);

            await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first-second", "first-second", "default"));

            // 1 invocation for create
            await Mock.WaitForInvocations;
            // 3 invocations: create, add finalizers
            await attachCounter.WaitForInvocations;
        }

        var oldInvocs = Mock.Invocations.ToList();

        // reset to catch 3 invocation: deleted, and 2 finalized
        Mock.Clear();
        Mock.TargetInvocationCount = 3;

        // 4 invocations: 1 when the watcher is created and the entity already exists, 2 for finalize, 1 for delete.
        var finalizeCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 4 };
        using (_client.Watch((_, e) => finalizeCounter.Invocation(e)))
        {
            await _client.DeleteAsync("first-second", "default");
            // 2 invocations: call to delete, update after finalize
            await finalizeCounter.WaitForInvocations;
            // 1 invocation for delete
            await Mock.WaitForInvocations;
        }

        oldInvocs.Should().Contain(i => i.Method == "ReconcileAsync");
        Mock.Invocations.Should().Contain(i => i.Method == "DeletedAsync");
        Mock.Invocations.Should().Contain(i => i.Method == "FinalizeAsync");
        Mock.Invocations.Count(i => i.Method == "FinalizeAsync").Should().Be(2);
    }

    public async Task InitializeAsync()
    {
        var meta = _mlc.ToEntityMetadata(typeof(V1OperatorIntegrationTestEntity)).Metadata;
        _client = new KubernetesClient<V1OperatorIntegrationTestEntity>(meta);
        await _hostBuilder.ConfigureAndStart(builder => builder.Services
            .AddSingleton(Mock)
            .AddKubernetesOperator()
            .AddController<TestController, V1OperatorIntegrationTestEntity>()
            .AddFinalizer<FirstFinalizer, V1OperatorIntegrationTestEntity>("first")
            .AddFinalizer<SecondFinalizer, V1OperatorIntegrationTestEntity>("second"));
    }

    public async Task DisposeAsync()
    {
        var entities = await _client.ListAsync("default");
        foreach (var e in entities)
        {
            if (e.Metadata.Finalizers is null)
            {
                continue;
            }

            e.Metadata.Finalizers.Clear();
            await _client.UpdateAsync(e);
        }

        await _client.DeleteAsync(entities);
        _client.Dispose();
    }

    private class TestController : IEntityController<V1OperatorIntegrationTestEntity>
    {
        private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _svc;
        private readonly EntityFinalizerAttacher<FirstFinalizer, V1OperatorIntegrationTestEntity> _first;
        private readonly EntityFinalizerAttacher<SecondFinalizer, V1OperatorIntegrationTestEntity> _second;

        public TestController(InvocationCounter<V1OperatorIntegrationTestEntity> svc,
            EntityFinalizerAttacher<FirstFinalizer, V1OperatorIntegrationTestEntity> first,
            EntityFinalizerAttacher<SecondFinalizer, V1OperatorIntegrationTestEntity> second)
        {
            _svc = svc;
            _first = first;
            _second = second;
        }

        public async Task ReconcileAsync(V1OperatorIntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            if (entity.Name().Contains("first"))
            {
                entity = await _first(entity);
            }

            if (entity.Name().Contains("second"))
            {
                await _second(entity);
            }
        }

        public Task DeletedAsync(V1OperatorIntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }

    private class FirstFinalizer : IEntityFinalizer<V1OperatorIntegrationTestEntity>
    {
        private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _svc;

        public FirstFinalizer(InvocationCounter<V1OperatorIntegrationTestEntity> svc)
        {
            _svc = svc;
        }

        public Task FinalizeAsync(V1OperatorIntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }

    private class SecondFinalizer : IEntityFinalizer<V1OperatorIntegrationTestEntity>
    {
        private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _svc;

        public SecondFinalizer(InvocationCounter<V1OperatorIntegrationTestEntity> svc)
        {
            _svc = svc;
        }

        public Task FinalizeAsync(V1OperatorIntegrationTestEntity entity)
        {
            _svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }
}
