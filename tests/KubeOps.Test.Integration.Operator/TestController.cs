using DotnetKubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Rbac;

namespace KubeOps.Test.Integration.Operator;

[EntityRbac(typeof(V2TestEntity), Verbs = RbacVerb.All)]
public class TestController : IResourceController<V2TestEntity>
{
    private readonly IKubernetesClient _client;

    public TestController(IKubernetesClient client)
    {
        _client = client;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V2TestEntity entity)
    {
        entity.Status.ReconcileCounter++;
        switch (entity.Spec.Spec)
        {
            case "testException" when entity.Status.ReconcileCounter < 4:
                await _client.UpdateStatus(entity);
                throw new ArgumentException(nameof(entity.Spec.Spec));
            case "testExceptionFail":
                await _client.UpdateStatus(entity);
                throw new ArgumentException(nameof(entity.Spec.Spec));
            case "testDoubleQueued":
                await _client.UpdateStatus(entity);
                return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5));
        }
        entity.Status.Status = "Updated";
        await _client.UpdateStatus(entity);
        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(1));
    }

    public Task StatusModifiedAsync(V2TestEntity entity)
    {
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V2TestEntity entity)
    {
        return Task.CompletedTask;
    }
}
