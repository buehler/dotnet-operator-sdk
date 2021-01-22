using System;
using System.Threading.Tasks;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Services;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.TestManager;

namespace KubeOps.TestOperator.Controller
{
    [EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
    public class TestController : ResourceControllerBase<V1TestEntity>
    {
        private readonly IManager _manager;

        public TestController(IManager manager, IResourceServices<V1TestEntity> services)
            : base(services)
        {
            _manager = manager;
        }

        protected override async Task<TimeSpan?> Created(V1TestEntity resource)
        {
            _manager.Created(resource);
            return await base.Created(resource);
        }

        protected override async Task<TimeSpan?> Updated(V1TestEntity resource)
        {
            _manager.Updated(resource);
            return await base.Updated(resource);
        }

        protected override async Task<TimeSpan?> NotModified(V1TestEntity resource)
        {
            _manager.NotModified(resource);
            return await base.NotModified(resource);
        }

        protected override async Task StatusModified(V1TestEntity resource)
        {
            _manager.StatusModified(resource);
            await base.StatusModified(resource);
        }

        protected override async Task Deleted(V1TestEntity resource)
        {
            _manager.Deleted(resource);
            await base.Deleted(resource);
        }
    }
}
