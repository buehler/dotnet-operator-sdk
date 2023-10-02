using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Rbac;

using Microsoft.Extensions.Logging;

using Operator.Entities;

namespace Operator.Controller;

[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
public class V1TestEntityController : IEntityController<V1TestEntity>
{
    private readonly ILogger<V1TestEntityController> _logger;

    public V1TestEntityController(ILogger<V1TestEntityController> logger)
    {
        _logger = logger;
    }

    public Task ReconcileAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Reconciling entity {Entity}.", entity);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Deleting entity {Entity}.", entity);
        return Task.CompletedTask;
    }
}
