using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
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

    private readonly IManagedResourceController _controller;
    private readonly Mock<IManager> _managerMock;

    public TestFinalizerTest(KubernetesOperatorFactory<TestStartup> factory)
    {
        _factory = factory.WithSolutionRelativeContentRoot("tests/KubeOps.TestOperator");

        _controller = _factory.Services
            .GetRequiredService<IControllerInstanceBuilder>()
            .BuildControllers<V1TestEntity>()
            .First();

        _managerMock = _factory.Services.GetRequiredService<Mock<IManager>>();
        _managerMock.Reset();
    }

    [Fact]
    public async Task Test_If_Manager_Finalizer_Is_Called()
    {
        _factory.Run();

        _managerMock.Setup(o => o.Finalized(It.IsAny<V1TestEntity>()));
        _managerMock.Verify(o => o.Finalized(It.IsAny<V1TestEntity>()), Times.Never);

        var testResource = new V1TestEntity
        {
            Metadata = new V1ObjectMeta
            {
                Finalizers = new List<string>
                {
                    (new TestEntityFinalizer(_managerMock.Object) as IResourceFinalizer<V1TestEntity>)
                    .Identifier,
                },
            },
        };

        await _controller.StartAsync();
        await _factory.EnqueueFinalization(testResource);
        await _controller.StopAsync();

        _managerMock.Verify(o => o.Finalized(It.IsAny<V1TestEntity>()), Times.Once);
    }
}
