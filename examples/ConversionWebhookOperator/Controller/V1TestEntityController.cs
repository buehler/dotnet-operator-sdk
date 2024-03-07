using ConversionWebhookOperator.Entities;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Rbac;

namespace ConversionWebhookOperator.Controller;

[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
public class V1TestEntityController(ILogger<V1TestEntityController> logger) : IEntityController<V1TestEntity>
{
    public Task ReconcileAsync(V1TestEntity entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling entity {Entity}.", entity);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1TestEntity entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleted entity {Entity}.", entity);
        return Task.CompletedTask;
    }
}
