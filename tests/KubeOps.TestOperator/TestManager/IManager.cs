using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator.TestManager
{
    public interface IManager
    {
        void Created(V1TestEntity entity);

        void Updated(V1TestEntity entity);

        void StatusModified(V1TestEntity entity);

        void NotModified(V1TestEntity entity);

        void Deleted(V1TestEntity entity);

        void Finalized(V1TestEntity entity);
    }
}
