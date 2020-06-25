using System.Threading.Tasks;
using KubeOps.Operator.Finalizer;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;

namespace KubeOps.TestOperator.Finalizer
{
    public class TestEntityFinalizer : ResourceFinalizerBase<TestEntity>
    {
        private readonly IManager _manager;

        public TestEntityFinalizer(IManager manager)
        {
            _manager = manager;
        }

        public override Task Finalize(TestEntity resource)
        {
            _manager.Finalized(resource);
            return Task.CompletedTask;
        }
    }
}
