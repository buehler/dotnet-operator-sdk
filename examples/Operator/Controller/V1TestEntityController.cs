using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
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
    private readonly EntityFinalizerAttacher<FinalizerOne, V1TestEntity> _finalizer1;
    private readonly EntityFinalizerAttacher<FinalizerTwo, V1TestEntity> _finalizer2;

    public V1TestEntityController(
        ILogger<V1TestEntityController> logger,
        IKubernetesClient<V1TestEntity> client,
        EntityFinalizerAttacher<FinalizerOne, V1TestEntity> finalizer1,
        EntityFinalizerAttacher<FinalizerTwo, V1TestEntity> finalizer2)
    {
        _logger = logger;
        _client = client;
        _finalizer1 = finalizer1;
        _finalizer2 = finalizer2;
    }

    public async Task ReconcileAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Reconciling entity {Entity}.", entity);

        entity = await _finalizer1(entity);
        entity = await _finalizer2(entity);

        entity.Status.Status = "Reconciling";
        entity = await _client.UpdateStatus(entity);
        entity.Status.Status = "Reconciled";
        await _client.UpdateStatus(entity);
    }

    public Task DeletedAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Deleting entity {Entity}.", entity);
        return Task.CompletedTask;
    }
}
