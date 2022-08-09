using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using GeneratedOperatorProject.Entities;
using GeneratedOperatorProject.Finalizer;

namespace GeneratedOperatorProject.Controller;

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

    public async Task<ResourceControllerResult?> ReconcileAsync(V1DemoEntity entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(ReconcileAsync)}.");
        await _finalizerManager.RegisterFinalizerAsync<DemoFinalizer>(entity);

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15));
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
