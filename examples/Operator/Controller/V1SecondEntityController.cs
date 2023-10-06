using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Events;
using KubeOps.Abstractions.Finalizer;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Logging;

using Operator.Entities;
using Operator.Finalizer;

namespace Operator.Controller;

[EntityRbac(typeof(V1SecondEntity), Verbs = RbacVerb.All)]
public class V1SecondEntityController : IEntityController<V1SecondEntity>
{
    private readonly ILogger<V1SecondEntityController> _logger;

    public V1SecondEntityController(
        ILogger<V1SecondEntityController> logger)
    {
        _logger = logger;
    }

    public Task ReconcileAsync(V1SecondEntity entity)
    {
        _logger.LogInformation("Reconciling entity {Entity}.", entity);
        return Task.CompletedTask;
    }
}
