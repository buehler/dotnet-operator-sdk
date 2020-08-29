using System.Threading.Tasks;
using KubeOps.Testing;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace KubeOps.TestOperator.Test
{
    public class TestControllerTest : IAsyncLifetime
    {
        private readonly Mock<IManager> _mock = new Mock<IManager>();

        private readonly KubernetesTestOperator _operator;

        public TestControllerTest()
        {
            _operator = new Operator()
                .ConfigureServices(
                    services =>
                    {
                        services.RemoveAll(typeof(IManager));
                        services.AddSingleton(typeof(IManager), _mock.Object);
                    })
                .ToKubernetesTestOperator();
        }

        [Fact]
        public async Task Test_If_Manager_Created_Is_Called()
        {
            await _operator.Run();
            _mock.Setup(o => o.Created(It.IsAny<TestEntity>()));
            _mock.Verify(o => o.Created(It.IsAny<TestEntity>()), Times.Never);
            _operator.MockedClient.UpdateResult = new TestEntity();
            var queue = _operator.GetMockedEventQueue<TestEntity>();
            queue.Created(new TestEntity());
            _mock.Verify(o => o.Created(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public async Task Test_If_Manager_Updated_Is_Called()
        {
            await _operator.Run();
            _mock.Setup(o => o.Updated(It.IsAny<TestEntity>()));
            _mock.Verify(o => o.Updated(It.IsAny<TestEntity>()), Times.Never);
            var queue = _operator.GetMockedEventQueue<TestEntity>();
            queue.Updated(new TestEntity());
            _mock.Verify(o => o.Updated(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public async Task Test_If_Manager_NotModified_Is_Called()
        {
            await _operator.Run();
            _mock.Setup(o => o.NotModified(It.IsAny<TestEntity>()));
            _mock.Verify(o => o.NotModified(It.IsAny<TestEntity>()), Times.Never);
            var queue = _operator.GetMockedEventQueue<TestEntity>();
            queue.NotModified(new TestEntity());
            _mock.Verify(o => o.NotModified(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public async Task Test_If_Manager_Deleted_Is_Called()
        {
            await _operator.Run();
            _mock.Setup(o => o.Deleted(It.IsAny<TestEntity>()));
            _mock.Verify(o => o.Deleted(It.IsAny<TestEntity>()), Times.Never);
            var queue = _operator.GetMockedEventQueue<TestEntity>();
            queue.Deleted(new TestEntity());
            _mock.Verify(o => o.Deleted(It.IsAny<TestEntity>()), Times.Once);
        }

        [Fact]
        public async Task Test_If_Manager_StatusModified_Is_Called()
        {
            await _operator.Run();
            _mock.Setup(o => o.StatusModified(It.IsAny<TestEntity>()));
            _mock.Verify(o => o.StatusModified(It.IsAny<TestEntity>()), Times.Never);
            var queue = _operator.GetMockedEventQueue<TestEntity>();
            queue.StatusUpdated(new TestEntity());
            _mock.Verify(o => o.StatusModified(It.IsAny<TestEntity>()), Times.Once);
        }

        public Task InitializeAsync()
            => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _operator.DisposeAsync();
        }
    }
}
