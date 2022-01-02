using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator.TestManager;

public interface IManager
{
    void Reconciled(V1TestEntity entity);

    void StatusModified(V1TestEntity entity);

    void Deleted(V1TestEntity entity);

    void Finalized(V1TestEntity entity);
}
