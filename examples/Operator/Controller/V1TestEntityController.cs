using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Logging;

using Operator.Entities;
using Operator.Finalizer;

namespace Operator.Controller;

[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
public class V1TestEntityController : IEntityController<V1TestEntity>
{
    private readonly ILogger<V1TestEntityController> _logger;
    private readonly IKubernetesClient<V1TestEntity> _client;
    private readonly EntityRequeue<V1TestEntity> _requeue;

    public V1TestEntityController(
        ILogger<V1TestEntityController> logger,
        IKubernetesClient<V1TestEntity> client,
        EntityRequeue<V1TestEntity> requeue)
    {
        _logger = logger;
        _client = client;
        _requeue = requeue;
    }

    public async Task ReconcileAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Reconciling entity {Entity}.", entity);

        entity.Status.Status = "Reconciled";
        await _client.UpdateStatusAsync(entity);

        _requeue(entity, TimeSpan.FromSeconds(5));
    }

    public Task DeletedAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Deleting entity {Entity}.", entity);
        return Task.CompletedTask;
    }
}
