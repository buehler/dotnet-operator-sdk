using KubeOps.Operator.Controller;
using KubeOps.Operator.Kubernetes;
using KubeOps.Testing;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KubeOps.TestOperator.Test;

public class TestControllerTest : IClassFixture<KubernetesOperatorFactory<TestStartup>>
{
    private readonly KubernetesOperatorFactory<TestStartup> _factory;

    private readonly IManagedResourceController _controller;
    private readonly Mock<IManager> _managerMock;

    public TestControllerTest(KubernetesOperatorFactory<TestStartup> factory)
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
    public async Task Test_If_Manager_Reconciled_Is_Called()
    {
        _factory.Run();

        _managerMock.Setup(o => o.Reconciled(It.IsAny<V1TestEntity>()));
        _managerMock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Never);

        await _controller.StartAsync();
        await _factory.EnqueueEvent(ResourceEventType.Reconcile, new V1TestEntity());
        await _controller.StopAsync();

        _managerMock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task Test_If_Manager_StatusModified_Is_Called()
    {
        _factory.Run();

        _managerMock.Setup(o => o.StatusModified(It.IsAny<V1TestEntity>()));
        _managerMock.Verify(o => o.StatusModified(It.IsAny<V1TestEntity>()), Times.Never);

        await _controller.StartAsync();
        await _factory.EnqueueEvent(ResourceEventType.StatusUpdated, new V1TestEntity());
        await _controller.StopAsync();

        _managerMock.Verify(o => o.StatusModified(It.IsAny<V1TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task Test_If_Manager_Deleted_Is_Called()
    {
        _factory.Run();

        _managerMock.Setup(o => o.Deleted(It.IsAny<V1TestEntity>()));
        _managerMock.Verify(o => o.Deleted(It.IsAny<V1TestEntity>()), Times.Never);

        await _controller.StartAsync();
        await _factory.EnqueueEvent(ResourceEventType.Deleted, new V1TestEntity());
        await _controller.StopAsync();

        _managerMock.Verify(o => o.Deleted(It.IsAny<V1TestEntity>()), Times.Once);
    }
}
