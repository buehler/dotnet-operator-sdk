using KubeOps.Operator;
using KubeOps.TestOperator.Controller;
using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator
{
    public static class Program
    {
        public static void Main(string[] args) => new KubernetesOperator()
            .ConfigureServices(services => { services.AddResourceController<TestController, TestEntity>(); })
            .Run(args);
    }
}
