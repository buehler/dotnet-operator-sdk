using KubeOps.Operator.Finalizer;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;

namespace KubeOps.TestOperator.Finalizer;

public class TestEntityFinalizer : IResourceFinalizer<V1TestEntity>
{
    private readonly IManager _manager;

    public TestEntityFinalizer(IManager manager)
    {
        _manager = manager;
    }

    public Task FinalizeAsync(V1TestEntity entity)
    {
        _manager.Finalized(entity);
        return Task.CompletedTask;
    }
}
