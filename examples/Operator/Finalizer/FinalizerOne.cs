using KubeOps.Abstractions.Finalizer;

using Operator.Entities;

namespace Operator.Finalizer;

public class FinalizerOne : IEntityFinalizer<V1TestEntity>
{
    public Task FinalizeAsync(V1TestEntity entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
