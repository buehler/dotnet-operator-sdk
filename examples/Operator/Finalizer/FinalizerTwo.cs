using KubeOps.Abstractions.Finalizer;

using Operator.Entities;

namespace Operator.Finalizer;

public class FinalizerTwo : IEntityFinalizer<V1TestEntity>
{
    public Task FinalizeAsync(V1TestEntity entity)
    {
        return Task.CompletedTask;
    }
}
