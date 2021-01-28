using System;
using System.Threading.Tasks;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.Finalizer;

namespace KubeOps.TestOperator.Controller
{
    [EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
    public class TestController : IResourceController<V1TestEntity>
    {
        private readonly IFinalizerManager<V1TestEntity> _finalizerManager;

        public TestController(IFinalizerManager<V1TestEntity> finalizerManager)
        {
            _finalizerManager = finalizerManager;
        }

        public async Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
        {
            await _finalizerManager.RegisterFinalizerAsync<TestEntityFinalizer>(resource);
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5));
        }

        public Task<ResourceControllerResult> NotModifiedAsync(V1TestEntity resource) =>
            Task.FromResult(ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5)));
    }
}
