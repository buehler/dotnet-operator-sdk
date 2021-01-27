using System;
using System.Threading.Tasks;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Rbac;
using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator.Controller
{
    [EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
    public class TestController : IResourceController<V1TestEntity>
    {
        public Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource) =>
            ResourceControllerResult.RequeueEventAsync(TimeSpan.FromSeconds(5));

        public Task<ResourceControllerResult> NotModifiedAsync(V1TestEntity resource) =>
            ResourceControllerResult.RequeueEventAsync(TimeSpan.FromSeconds(5));
    }
}
