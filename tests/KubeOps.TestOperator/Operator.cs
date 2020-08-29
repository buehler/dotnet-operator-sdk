using KubeOps.Operator;
using KubeOps.TestOperator.Controller;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.TestOperator
{
    public class Operator : KubernetesOperator
    {
        public Operator()
        {
            ConfigureServices(
                services =>
                {
                    services.AddTransient<IManager, TestManager.TestManager>();
                    services.AddResourceController<TestController>();
                    services.AddResourceFinalizer<TestEntityFinalizer, TestEntity>();
                });
        }
    }
}
