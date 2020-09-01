using KubeOps.Testing;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KubeOps.TestOperator.Test
{
    public class TestControllerTest : IClassFixture<KubernetesOperatorFactory<TestStartup>>
    {
        private readonly KubernetesOperatorFactory<TestStartup> _factory;

        public TestControllerTest(KubernetesOperatorFactory<TestStartup> factory)
        {
            _factory = factory.WithSolutionRelativeContentRoot("tests/KubeOps.TestOperator");
        }

        [Fact]
        public void Test_If_Manager_Created_Is_Called()
        {
            _factory.Run();
            var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
            mock.Reset();
            mock.Setup(o => o.Created(It.IsAny<TestEntity>()));
            mock.Verify(o => o.Created(It.IsAny<TestEntity>()), Times.Never);
            _factory.MockedKubernetesClient.UpdateResult = new TestEntity();
            var queue = _factory.GetMockedEventQueue<TestEntity>();
            queue.Created(new TestEntity());
            mock.Verify(o => o.Created(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public void Test_If_Manager_Updated_Is_Called()
        {
            _factory.Run();
            var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
            mock.Reset();
            mock.Setup(o => o.Updated(It.IsAny<TestEntity>()));
            mock.Verify(o => o.Updated(It.IsAny<TestEntity>()), Times.Never);
            var queue = _factory.GetMockedEventQueue<TestEntity>();
            queue.Updated(new TestEntity());
            mock.Verify(o => o.Updated(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public void Test_If_Manager_NotModified_Is_Called()
        {
            _factory.Run();
            var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
            mock.Reset();
            mock.Setup(o => o.NotModified(It.IsAny<TestEntity>()));
            mock.Verify(o => o.NotModified(It.IsAny<TestEntity>()), Times.Never);
            var queue = _factory.GetMockedEventQueue<TestEntity>();
            queue.NotModified(new TestEntity());
            mock.Verify(o => o.NotModified(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public void Test_If_Manager_Deleted_Is_Called()
        {
            _factory.Run();
            var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
            mock.Reset();
            mock.Setup(o => o.Deleted(It.IsAny<TestEntity>()));
            mock.Verify(o => o.Deleted(It.IsAny<TestEntity>()), Times.Never);
            var queue = _factory.GetMockedEventQueue<TestEntity>();
            queue.Deleted(new TestEntity());
            mock.Verify(o => o.Deleted(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public void Test_If_Manager_StatusModified_Is_Called()
        {
            _factory.Run();
            var mock = _factory.Services.GetRequiredService<Mock<IManager>>();
            mock.Reset();
            mock.Setup(o => o.StatusModified(It.IsAny<TestEntity>()));
            mock.Verify(o => o.StatusModified(It.IsAny<TestEntity>()), Times.Never);
            var queue = _factory.GetMockedEventQueue<TestEntity>();
            queue.StatusUpdated(new TestEntity());
            mock.Verify(o => o.StatusModified(It.IsAny<TestEntity>()), Times.Once);
        }
    }
}
