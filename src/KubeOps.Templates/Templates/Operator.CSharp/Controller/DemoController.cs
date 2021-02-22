using System;
using System.Threading.Tasks;
using GeneratedOperatorProject.Entities;
using GeneratedOperatorProject.Finalizer;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using Microsoft.Extensions.Logging;

namespace GeneratedOperatorProject.Controller
{
    [EntityRbac(typeof(V1DemoEntity), Verbs = RbacVerb.All)]
    public class DemoController : IResourceController<V1DemoEntity>
    {
        private readonly ILogger<DemoController> _logger;
        private readonly IFinalizerManager<V1DemoEntity> _finalizerManager;

        public DemoController(ILogger<DemoController> logger, IFinalizerManager<V1DemoEntity> finalizerManager)
        {
            _logger = logger;
            _finalizerManager = finalizerManager;
        }

        public async Task<ResourceControllerResult?> CreatedAsync(V1DemoEntity entity)
        {
            _logger.LogInformation($"entity {entity.Name()} called {nameof(CreatedAsync)}.");
            await _finalizerManager.RegisterFinalizerAsync<DemoFinalizer>(entity);

            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5));
        }

        public Task<ResourceControllerResult?> UpdatedAsync(V1DemoEntity entity)
        {
            _logger.LogInformation($"entity {entity.Name()} called {nameof(UpdatedAsync)}.");

            return Task.FromResult<ResourceControllerResult?>(
                ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5)));
        }

        public Task<ResourceControllerResult?> NotModifiedAsync(V1DemoEntity entity)
        {
            _logger.LogInformation($"entity {entity.Name()} called {nameof(NotModifiedAsync)}.");

            return Task.FromResult<ResourceControllerResult?>(
                ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5)));
        }

        public Task StatusModifiedAsync(V1DemoEntity entity)
        {
            _logger.LogInformation($"entity {entity.Name()} called {nameof(StatusModifiedAsync)}.");

            return Task.CompletedTask;
        }

        public Task DeletedAsync(V1DemoEntity entity)
        {
            _logger.LogInformation($"entity {entity.Name()} called {nameof(DeletedAsync)}.");

            return Task.CompletedTask;
        }
    }
}
