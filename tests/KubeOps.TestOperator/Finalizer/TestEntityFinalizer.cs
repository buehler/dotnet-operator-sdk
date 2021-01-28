using System.Threading.Tasks;
using KubeOps.Operator.Finalizer;
using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator.Finalizer
{
    public class TestEntityFinalizer : IResourceFinalizer<V1TestEntity>
    {
        public Task FinalizeAsync(V1TestEntity resource) => Task.CompletedTask;
    }
}
