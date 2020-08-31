using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;

namespace KubeOps.Operator
{
    public interface IOperatorBuilder
    {
        OperatorBuilder AddController<T>()
            where T : class, IResourceController;
    }
}
