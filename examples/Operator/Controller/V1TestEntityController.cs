using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Events;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;

using Microsoft.Extensions.Logging;

using Operator.Entities;

namespace Operator.Controller;

[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
public class V1TestEntityController(ILogger<V1TestEntityController> logger,
        EntityRequeue<V1TestEntity> requeue,
        EventPublisher eventPublisher)
    : IEntityController<V1TestEntity>
{
    public async Task ReconcileAsync(V1TestEntity entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling entity {Entity}.", entity);

        await eventPublisher(entity, "RECONCILED", "Entity was reconciled.");

        requeue(entity, TimeSpan.FromSeconds(5));
    }

    public Task DeletedAsync(V1TestEntity entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting entity {Entity}.", entity);
        return Task.CompletedTask;
    }
}
