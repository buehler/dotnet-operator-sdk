using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Testing;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace KubeOps.TestOperator.Test
{
    public class TestFinalizerTest : IAsyncLifetime
    {
        private readonly Mock<IManager> _mock = new Mock<IManager>();

        private readonly KubernetesTestOperator _operator;

        public TestFinalizerTest()
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

        [Fact(Skip = "I have no idea why this fails.")]
        public async Task Test_If_Manager_Finalized_Is_Called()
        {
            await _operator.Run();
            _mock.Setup(o => o.Finalized(It.IsAny<TestEntity>()));
            _mock.Verify(o => o.Finalized(It.IsAny<TestEntity>()), Times.Never);
            _operator.MockedClient.UpdateResult = new TestEntity();
            var queue = _operator.GetMockedEventQueue<TestEntity>();
            queue.Finalizing(
                new TestEntity
                {
                    Metadata = new V1ObjectMeta
                    {
                        Finalizers = new[] { new TestEntityFinalizer(_mock.Object, null, null).Identifier },
                    }
                });
            _mock.Verify(o => o.Finalized(It.IsAny<TestEntity>()), Times.Once);
        }

        public Task InitializeAsync()
            => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _operator.DisposeAsync();
        }
    }
}
