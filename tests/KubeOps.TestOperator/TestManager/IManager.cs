using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator.TestManager
{
    public interface IManager
    {
        void Created(TestEntity entity);

        void Updated(TestEntity entity);

        void StatusModified(TestEntity entity);

        void NotModified(TestEntity entity);

        void Deleted(TestEntity entity);
    }
}
