using System;
using System.Threading.Tasks;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;

namespace KubeOps.TestOperator.Controller
{
    [EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
    public class TestController : IResourceController<V1TestEntity>
    {
        private readonly IManager _manager;

        public TestController(IManager manager)
        {
            _manager = manager;
        }

        public Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
        {
            _manager.Created(resource);
            return Task.FromResult<ResourceControllerResult>(null);
        }

        public Task<ResourceControllerResult> UpdatedAsync(V1TestEntity resource)
        {
            _manager.Updated(resource);
            return Task.FromResult<ResourceControllerResult>(null);
        }

        public Task<ResourceControllerResult> NotModifiedAsync(V1TestEntity resource)
        {
            _manager.NotModified(resource);
            return Task.FromResult<ResourceControllerResult>(null);
        }

        public Task StatusModifiedAsync(V1TestEntity resource)
        {
            _manager.StatusModified(resource);
            return Task.CompletedTask;
        }

        public Task DeletedAsync(V1TestEntity resource)
        {
            _manager.Deleted(resource);
            return Task.CompletedTask;
        }
    }
}
