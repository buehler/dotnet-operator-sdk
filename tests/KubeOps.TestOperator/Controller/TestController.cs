using KubeOps.Operator.Controller;
using KubeOps.Operator.Rbac;
using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator.Controller
{
    [EntityRbac(typeof(TestEntity), Verbs = RbacVerb.All)]
    public class TestController : ResourceControllerBase<TestEntity>
    {
    }
}
