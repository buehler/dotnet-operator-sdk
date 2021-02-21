using System.Threading.Tasks;
using GeneratedOperatorProject.Entities;
using k8s.Models;
using KubeOps.Operator.Finalizer;
using Microsoft.Extensions.Logging;

namespace GeneratedOperatorProject.Finalizer
{
    public class DemoFinalizer : IResourceFinalizer<V1DemoEntity>
    {
        private readonly ILogger<DemoFinalizer> _logger;

        public DemoFinalizer(ILogger<DemoFinalizer> logger)
        {
            _logger = logger;
        }

        public Task FinalizeAsync(V1DemoEntity entity)
        {
            _logger.LogInformation($"entity {entity.Name()} called {nameof(FinalizeAsync)}.");

            return Task.CompletedTask;
        }
    }
}
