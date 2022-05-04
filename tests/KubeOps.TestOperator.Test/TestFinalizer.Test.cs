using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Kubernetes;
using KubeOps.Testing;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KubeOps.TestOperator.Test;

public class TestFinalizerTest : IClassFixture<KubernetesOperatorFactory<TestStartup>>
{
    private readonly KubernetesOperatorFactory<TestStartup> _factory;

    public TestFinalizerTest(KubernetesOperatorFactory<TestStartup> factory)
    {
        _factory = factory.WithSolutionRelativeContentRoot("tests/KubeOps.TestOperator");
    }

    private IAsyncDisposable RunScoped<TEntity>(
        out IManagedResourceController controller,
        out IEventQueue<TEntity> eventQueue,
        out Mock<IManager> mockManager)
        where TEntity : class, IKubernetesObject<V1ObjectMeta>
    {
        var scope = _factory.Services.CreateAsyncScope();

        controller = scope.ServiceProvider
            .GetRequiredService<IControllerInstanceBuilder>()
            .BuildControllers<TEntity>()
            .First();
        eventQueue = scope.ServiceProvider
            .GetRequiredService<IEventQueue<TEntity>>();

        mockManager = _factory.Services.GetRequiredService<Mock<IManager>>();

        return scope;
    }

    [Fact]
    public async Task Test_If_Manager_Finalizer_Is_Called()
    {
        _factory.Run();

        await using var scope = RunScoped<V1TestEntity>(out var controller, out var eventQueue, out var mockManager);

        mockManager.Reset();
        mockManager.Setup(o => o.Finalized(It.IsAny<V1TestEntity>()));
        mockManager.Verify(o => o.Finalized(It.IsAny<V1TestEntity>()), Times.Never);

        var testResource = new V1TestEntity
        {
            Metadata = new V1ObjectMeta
            {
                Finalizers = new List<string>
                {
                    (new TestEntityFinalizer(mockManager.Object) as IResourceFinalizer<V1TestEntity>)
                    .Identifier,
                },
            },
        };
        var testEvent = new ResourceEvent<V1TestEntity>(ResourceEventType.Finalizing, testResource);

        await controller.StartAsync();
        eventQueue.EnqueueLocal(testEvent);

        mockManager.Verify(o => o.Finalized(It.IsAny<V1TestEntity>()), Times.Once);
    }
}
