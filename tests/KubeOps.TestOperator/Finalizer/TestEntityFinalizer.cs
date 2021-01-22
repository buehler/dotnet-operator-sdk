using System.Threading.Tasks;
using DotnetKubernetesClient;
using KubeOps.Operator.Finalizer;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.Logging;

namespace KubeOps.TestOperator.Finalizer
{
    public class TestEntityFinalizer : ResourceFinalizerBase<V1TestEntity>
    {
        private readonly IManager _manager;

        public TestEntityFinalizer(
            IManager manager,
            IKubernetesClient client,
            ILogger<ResourceFinalizerBase<V1TestEntity>> logger)
            : base(logger, client)
        {
            _manager = manager;
        }

        public override Task Finalize(V1TestEntity resource)
        {
            _manager.Finalized(resource);
            return Task.CompletedTask;
        }
    }
}
