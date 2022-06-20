using System;
using System.Threading.Tasks;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Rbac;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;

namespace KubeOps.TestOperator.Controller;

[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
public class TestController : IResourceController<V1TestEntity>
{
    private readonly IManager _manager;

    public TestController(IManager manager)
    {
        _manager = manager;
    }

    public async Task<ResourceControllerResult> ReconcileAsync(V1TestEntity entity)
    {
        _manager.Reconciled(entity);
        await Task.Delay(5000);
        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5));
    }

    public Task StatusModifiedAsync(V1TestEntity entity)
    {
        _manager.StatusModified(entity);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1TestEntity entity)
    {
        _manager.Deleted(entity);
        return Task.CompletedTask;
    }
}
