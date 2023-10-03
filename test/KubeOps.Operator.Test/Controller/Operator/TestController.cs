using KubeOps.Abstractions.Controller;
using KubeOps.Operator.Test.TestEntities;

namespace KubeOps.Operator.Test.Controller.Operator;

public class TestController : IEntityController<V1IntegrationTestEntity>
{
    private readonly InvocationCounter<V1IntegrationTestEntity> _svc;

    public TestController(InvocationCounter<V1IntegrationTestEntity> svc)
    {
        _svc = svc;
    }

    public Task ReconcileAsync(V1IntegrationTestEntity entity)
    {
        _svc.Invocation(entity);
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1IntegrationTestEntity entity)
    {
        _svc.Invocation(entity);
        return Task.CompletedTask;
    }
}
