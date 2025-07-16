﻿using FluentAssertions;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Events;
using KubeOps.Abstractions.Finalizer;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;
using KubeOps.KubernetesClient.LabelSelectors;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Test.TestEntities;
using KubeOps.Operator.Watcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Test.Builder;

public class OperatorBuilderTest
{
    private readonly IOperatorBuilder _builder = new OperatorBuilder(new ServiceCollection(), new());

    [Fact]
    public void Should_Add_Default_Resources()
    {
        // Add controllers to trigger the label selector registrations
        _builder.AddController<TestControllerWithSelector<TestLabelSelector>, V1OperatorIntegrationTestEntity, TestLabelSelector>();
        _builder.AddController<TestControllerWithSelector<TestLabelSelector2>, V1OperatorIntegrationTestEntity, TestLabelSelector2>();

        // This test verifies the basic services that are registered
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(OperatorSettings) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(EventPublisher) &&
            s.Lifetime == ServiceLifetime.Transient);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEventPublisherFactory) &&
            s.Lifetime == ServiceLifetime.Transient);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IKubernetesClient) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector>) &&
            s.ImplementationType == typeof(TestLabelSelector) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector2>) &&
            s.ImplementationType == typeof(TestLabelSelector2) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void Should_Use_Specific_EntityLabelSelector_Implementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register the default and specific implementations
        services.TryAddSingleton<IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector>, TestLabelSelector>();
        services.TryAddSingleton<IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector2>, TestLabelSelector2>();


        var serviceProvider = services.BuildServiceProvider();

        {
            // Act
            var resolvedService = serviceProvider.GetRequiredService<IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector>>();

            // Assert
            Assert.IsType<TestLabelSelector>(resolvedService);
        }

        {
            // Act
            var resolvedService = serviceProvider.GetRequiredService<IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector2>>();

            // Assert
            Assert.IsType<TestLabelSelector2>(resolvedService);
        }
    }

    [Fact]
    public void Should_Add_Controller_Resources()
    {
        _builder.AddController<TestController, V1OperatorIntegrationTestEntity>();

        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEntityController<V1OperatorIntegrationTestEntity, DefaultEntityLabelSelector<V1OperatorIntegrationTestEntity>>) &&
            s.ImplementationType == typeof(TestController) &&
            s.Lifetime == ServiceLifetime.Scoped);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(ResourceWatcher<V1OperatorIntegrationTestEntity, DefaultEntityLabelSelector<V1OperatorIntegrationTestEntity>>) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(TimedEntityQueue<V1OperatorIntegrationTestEntity>) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(EntityRequeue<V1OperatorIntegrationTestEntity>) &&
            s.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Should_Add_Controller_Resources_With_Label_Selector()
    {
        _builder.AddController<TestControllerWithSelector<TestLabelSelector>, V1OperatorIntegrationTestEntity, TestLabelSelector>();
        _builder.AddController<TestControllerWithSelector<TestLabelSelector2>, V1OperatorIntegrationTestEntity, TestLabelSelector2>();

        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEntityController<V1OperatorIntegrationTestEntity, TestLabelSelector>) &&
            s.ImplementationType == typeof(TestControllerWithSelector<TestLabelSelector>) &&
            s.Lifetime == ServiceLifetime.Scoped);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEntityController<V1OperatorIntegrationTestEntity, TestLabelSelector2>) &&
            s.ImplementationType == typeof(TestControllerWithSelector<TestLabelSelector2>) &&
            s.Lifetime == ServiceLifetime.Scoped);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(ResourceWatcher<V1OperatorIntegrationTestEntity, TestLabelSelector>) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(ResourceWatcher<V1OperatorIntegrationTestEntity, TestLabelSelector2>) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(TimedEntityQueue<V1OperatorIntegrationTestEntity>) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(EntityRequeue<V1OperatorIntegrationTestEntity>) &&
            s.Lifetime == ServiceLifetime.Transient);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector>) &&
            s.ImplementationType == typeof(TestLabelSelector) &&
            s.Lifetime == ServiceLifetime.Singleton);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector2>) &&
            s.ImplementationType == typeof(TestLabelSelector2) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void Should_Add_Finalizer_Resources()
    {
        _builder.AddFinalizer<TestFinalizer, V1OperatorIntegrationTestEntity>(string.Empty);

        _builder.Services.Should().Contain(s =>
            s.IsKeyedService &&
            s.KeyedImplementationType == typeof(TestFinalizer) &&
            s.Lifetime == ServiceLifetime.Transient);
        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(EntityFinalizerAttacher<TestFinalizer, V1OperatorIntegrationTestEntity>) &&
            s.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Should_Add_Leader_Elector()
    {
        var builder = new OperatorBuilder(new ServiceCollection(), new() { EnableLeaderElection = true });
        builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(k8s.LeaderElection.LeaderElector) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void Should_Add_LeaderAwareResourceWatcher()
    {
        var builder = new OperatorBuilder(new ServiceCollection(), new() { EnableLeaderElection = true });
        builder.AddController<TestController, V1OperatorIntegrationTestEntity>();

        builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(LeaderAwareResourceWatcher<V1OperatorIntegrationTestEntity, DefaultEntityLabelSelector<V1OperatorIntegrationTestEntity>>) &&
            s.Lifetime == ServiceLifetime.Singleton);
        builder.Services.Should().NotContain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(ResourceWatcher<V1OperatorIntegrationTestEntity, DefaultEntityLabelSelector<V1OperatorIntegrationTestEntity>>) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }

    private class TestController : IEntityController<V1OperatorIntegrationTestEntity>
    {
        public Task ReconcileAsync(V1OperatorIntegrationTestEntity entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeletedAsync(V1OperatorIntegrationTestEntity entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private class TestControllerWithSelector<TSelector> : IEntityController<V1OperatorIntegrationTestEntity, TSelector>
        where TSelector : class, IEntityLabelSelector<V1OperatorIntegrationTestEntity, TSelector>
    {
        public Task ReconcileAsync(V1OperatorIntegrationTestEntity entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeletedAsync(V1OperatorIntegrationTestEntity entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private class TestFinalizer : IEntityFinalizer<V1OperatorIntegrationTestEntity>
    {
        public Task FinalizeAsync(V1OperatorIntegrationTestEntity entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private class TestLabelSelector : IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector>
    {
        public ValueTask<string?> GetLabelSelectorAsync(CancellationToken cancellationToken)
        {
            var labelSelectors = new LabelSelector[]
            {
                new EqualsSelector("label", "value")
            };

            return ValueTask.FromResult<string?>(labelSelectors.ToExpression());
        }
    }

    private class TestLabelSelector2 : IEntityLabelSelector<V1OperatorIntegrationTestEntity, TestLabelSelector2>
    {
        public ValueTask<string?> GetLabelSelectorAsync(CancellationToken cancellationToken)
        {
            var labelSelectors = new LabelSelector[]
            {
                new EqualsSelector("label", "value"),
                new EqualsSelector("label2", "value")
            };

            return ValueTask.FromResult<string?>(labelSelectors.ToExpression());
        }
    }
}
