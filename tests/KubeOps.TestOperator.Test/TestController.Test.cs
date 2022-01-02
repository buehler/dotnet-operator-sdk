using System.Threading.Tasks;
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

    public TestControllerTest(KubernetesOperatorFactory<TestStartup> factory)
    {
        _factory = factory.WithSolutionRelativeContentRoot("tests/KubeOps.TestOperator");
    }

    [Fact]
    public async Task Test_If_Manager_Created_Is_Called()
    {
        _factory.Run();
        var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
        mock.Reset();
        mock.Setup(o => o.Reconciled(It.IsAny<V1TestEntity>()));
        mock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Never);
        await _factory.EnqueueEvent(ResourceEventType.Reconcile, new V1TestEntity());
        mock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task Test_If_Manager_Updated_Is_Called()
    {
        _factory.Run();
        var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
        mock.Reset();
        mock.Setup(o => o.Reconciled(It.IsAny<V1TestEntity>()));
        mock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Never);
        await _factory.EnqueueEvent(ResourceEventType.Reconcile, new V1TestEntity());
        mock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task Test_If_Manager_NotModified_Is_Called()
    {
        _factory.Run();
        var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
        mock.Reset();
        mock.Setup(o => o.Reconciled(It.IsAny<V1TestEntity>()));
        mock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Never);
        await _factory.EnqueueEvent(ResourceEventType.Reconcile, new V1TestEntity());
        mock.Verify(o => o.Reconciled(It.IsAny<V1TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task Test_If_Manager_StatusModified_Is_Called()
    {
        _factory.Run();
        var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
        mock.Reset();
        mock.Setup(o => o.StatusModified(It.IsAny<V1TestEntity>()));
        mock.Verify(o => o.StatusModified(It.IsAny<V1TestEntity>()), Times.Never);
        await _factory.EnqueueEvent(ResourceEventType.StatusUpdated, new V1TestEntity());
        mock.Verify(o => o.StatusModified(It.IsAny<V1TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task Test_If_Manager_Deleted_Is_Called()
    {
        _factory.Run();
        var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
        mock.Reset();
        mock.Setup(o => o.Deleted(It.IsAny<V1TestEntity>()));
        mock.Verify(o => o.Deleted(It.IsAny<V1TestEntity>()), Times.Never);
        await _factory.EnqueueEvent(ResourceEventType.Deleted, new V1TestEntity());
        mock.Verify(o => o.Deleted(It.IsAny<V1TestEntity>()), Times.Once);
    }
}
