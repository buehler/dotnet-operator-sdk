using System;
using System.Threading.Tasks;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Events;
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
        private readonly IEventManager.Publisher _publisher;

        public TestController(IManager manager, IEventManager eventManager, IResourceServices<V1TestEntity> services)
            : base(services)
        {
            _manager = manager;
            _publisher = eventManager.CreatePublisher("reason", "my fancy message");
        }

        protected override async Task<TimeSpan?> Created(V1TestEntity resource)
        {
            _manager.Created(resource);
            await _publisher(resource);
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
