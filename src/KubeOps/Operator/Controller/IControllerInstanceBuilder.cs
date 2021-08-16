using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Controller
{
    internal interface IControllerInstanceBuilder
    {
        public IEnumerable<IManagedResourceController> BuildControllers();

        public IEnumerable<IManagedResourceController> BuildControllers<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>;
    }
}
