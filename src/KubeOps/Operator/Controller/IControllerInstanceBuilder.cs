using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;

namespace KubeOps.Operator.Controller;

internal interface IControllerInstanceBuilder
{
    public delegate ScopedResourceController ControllerFactory(
        IServiceProvider parent,
        IComponentRegistrar.ControllerRegistration controllerRegistration);

    public IEnumerable<ScopedResourceController> BuildControllers();

    public IEnumerable<ScopedResourceController> BuildControllers<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
