﻿using FluentAssertions;

using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Test.TestEntities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Test.Finalizer;

public class EntityFinalizerIntegrationTest : IntegrationTestBase
{
    private readonly InvocationCounter<V1OperatorIntegrationTestEntity> _mock = new();
    private readonly IKubernetesClient _client = new KubernetesClient.KubernetesClient();
    private readonly TestNamespaceProvider _ns = new();

    [Fact]
    public async Task Should_Not_Call_Controller_When_Attaching_Finalizer()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 2 };
        using var watcher =
            _client.Watch<V1OperatorIntegrationTestEntity>((_, e) => watcherCounter.Invocation(e),
                @namespace: _ns.Namespace);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first", "first", _ns.Namespace));
        await _mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        _mock.Invocations.Count.Should().Be(1);
        // the resource watcher will be called twice (since the finalizer is added).
        watcherCounter.Invocations.Count.Should().Be(2);
        var (method, entity) = _mock.Invocations[0];
        method.Should().Be("ReconcileAsync");
        entity.Name().Should().Be("first");
    }

    [Fact]
    public async Task Should_Not_Call_Controller_When_Attaching_Finalizers()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using var watcher =
            _client.Watch<V1OperatorIntegrationTestEntity>((_, e) => watcherCounter.Invocation(e),
                @namespace: _ns.Namespace);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first-second", "first-second", _ns.Namespace));
        await _mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        _mock.Invocations.Count.Should().Be(1);
        // the resource watcher will be called trice (since the finalizers are added).
        watcherCounter.Invocations.Count.Should().Be(3);
        var (method, entity) = _mock.Invocations[0];
        method.Should().Be("ReconcileAsync");
        entity.Name().Should().Be("first-second");
    }

    [Fact]
    public async Task Should_Attach_Finalizer_On_Entity()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 2 };
        using var watcher =
            _client.Watch<V1OperatorIntegrationTestEntity>((_, e) => watcherCounter.Invocation(e),
                @namespace: _ns.Namespace);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first", "first", _ns.Namespace));
        await _mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        var result = await _client.GetAsync<V1OperatorIntegrationTestEntity>("first", _ns.Namespace);
        result!.Metadata.Finalizers.Should().Contain("first");
    }

    [Fact]
    public async Task Should_Attach_Multiple_Finalizer_On_Entity()
    {
        var watcherCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using var watcher =
            _client.Watch<V1OperatorIntegrationTestEntity>((_, e) => watcherCounter.Invocation(e),
                @namespace: _ns.Namespace);

        await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first-second", "first-second", _ns.Namespace));
        await _mock.WaitForInvocations;
        await watcherCounter.WaitForInvocations;

        var result = await _client.GetAsync<V1OperatorIntegrationTestEntity>("first-second", _ns.Namespace);
        result!.Metadata.Finalizers.Should().Contain("first");
        result.Metadata.Finalizers.Should().Contain("second");
    }

    [Fact]
    public async Task Should_Finalize_And_Delete_An_Entity()
    {
        var attachCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 2 };
        using (_client.Watch<V1OperatorIntegrationTestEntity>((_, e) => attachCounter.Invocation(e),
                   @namespace: _ns.Namespace))
        {
            await _client.CreateAsync(new V1OperatorIntegrationTestEntity("first", "first", _ns.Namespace));

            // 1 invocation for create
            await _mock.WaitForInvocations;
            // 2 invocations: create, add finalizer
            await attachCounter.WaitForInvocations;
        }

        var oldInvocs = _mock.Invocations.ToList();

        // reset to catch 1 invocation: deleted
        _mock.Clear();

        // 3 invocations: 1 when the watcher is created and the entity already exists, 1 for finalize, 1 for delete.
        var finalizeCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using (_client.Watch<V1OperatorIntegrationTestEntity>((_, e) => finalizeCounter.Invocation(e),
                   @namespace: _ns.Namespace))
        {
            await _client.DeleteAsync<V1OperatorIntegrationTestEntity>("first", _ns.Namespace);
            // 2 invocations: call to delete, update after finalize
            await finalizeCounter.WaitForInvocations;
            // 1 invocation for delete
            await _mock.WaitForInvocations;
        }

        oldInvocs.Should().Contain(i => i.Method == "ReconcileAsync");
        _mock.Invocations.Should().Contain(i => i.Method == "DeletedAsync");
        _mock.Invocations.Should().Contain(i => i.Method == "FinalizeAsync");
    }

    [Fact]
    public async Task Should_Finalize_And_Delete_An_Entity_With_Multiple_Finalizer()
    {
        var attachCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 3 };
        using (_client.Watch<V1OperatorIntegrationTestEntity>((_, e) => attachCounter.Invocation(e),
                   @namespace: _ns.Namespace))
        {
            await _client.CreateAsync(
                new V1OperatorIntegrationTestEntity("first-second", "first-second", _ns.Namespace));

            // 1 invocation for create
            await _mock.WaitForInvocations;
            // 3 invocations: create, add finalizers
            await attachCounter.WaitForInvocations;
        }

        var oldInvocs = _mock.Invocations.ToList();

        // reset to catch 3 invocation: deleted, and 2 finalized
        _mock.Clear();
        _mock.TargetInvocationCount = 3;

        // 4 invocations: 1 when the watcher is created and the entity already exists, 2 for finalize, 1 for delete.
        var finalizeCounter = new InvocationCounter<V1OperatorIntegrationTestEntity> { TargetInvocationCount = 4 };
        using (_client.Watch<V1OperatorIntegrationTestEntity>((_, e) => finalizeCounter.Invocation(e),
                   @namespace: _ns.Namespace))
        {
            await _client.DeleteAsync<V1OperatorIntegrationTestEntity>("first-second", _ns.Namespace);
            // 2 invocations: call to delete, update after finalize
            await finalizeCounter.WaitForInvocations;
            // 1 invocation for delete
            await _mock.WaitForInvocations;
        }

        oldInvocs.Should().Contain(i => i.Method == "ReconcileAsync");
        _mock.Invocations.Should().Contain(i => i.Method == "DeletedAsync");
        _mock.Invocations.Should().Contain(i => i.Method == "FinalizeAsync");
        _mock.Invocations.Count(i => i.Method == "FinalizeAsync").Should().Be(2);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await _ns.InitializeAsync();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        var entities = await _client.ListAsync<V1OperatorIntegrationTestEntity>(_ns.Namespace);
        foreach (var e in entities)
        {
            if (e.Metadata.Finalizers is null)
            {
                continue;
            }

            e.Metadata.Finalizers.Clear();
            await _client.UpdateAsync(e);
        }

        await _ns.DisposeAsync();
        _client.Dispose();
    }

    protected override void ConfigureHost(HostApplicationBuilder builder)
    {
        builder.Services
            .AddSingleton(_mock)
            .AddKubernetesOperator(s => s.Namespace = _ns.Namespace)
            .AddController<TestController, V1OperatorIntegrationTestEntity>()
            .AddFinalizer<FirstFinalizer, V1OperatorIntegrationTestEntity>("first")
            .AddFinalizer<SecondFinalizer, V1OperatorIntegrationTestEntity>("second");
    }

    private class TestController(InvocationCounter<V1OperatorIntegrationTestEntity> svc,
            EntityFinalizerAttacher<FirstFinalizer, V1OperatorIntegrationTestEntity> first,
            EntityFinalizerAttacher<SecondFinalizer, V1OperatorIntegrationTestEntity> second)
        : IEntityController<V1OperatorIntegrationTestEntity>
    {
        public async Task ReconcileAsync(V1OperatorIntegrationTestEntity entity)
        {
            svc.Invocation(entity);
            if (entity.Name().Contains("first"))
            {
                entity = await first(entity);
            }

            if (entity.Name().Contains("second"))
            {
                await second(entity);
            }
        }

        public Task DeletedAsync(V1OperatorIntegrationTestEntity entity)
        {
            svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }

    private class FirstFinalizer(InvocationCounter<V1OperatorIntegrationTestEntity> svc) : IEntityFinalizer<V1OperatorIntegrationTestEntity>
    {
        public Task FinalizeAsync(V1OperatorIntegrationTestEntity entity)
        {
            svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }

    private class SecondFinalizer(InvocationCounter<V1OperatorIntegrationTestEntity> svc) : IEntityFinalizer<V1OperatorIntegrationTestEntity>
    {
        public Task FinalizeAsync(V1OperatorIntegrationTestEntity entity)
        {
            svc.Invocation(entity);
            return Task.CompletedTask;
        }
    }
}
