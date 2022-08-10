using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;

namespace KubeOps.Operator.Controller;

internal class ControllerInstanceBuilder : IControllerInstanceBuilder
{
    private readonly IComponentRegistrar _componentRegistrar;
    private readonly IControllerInstanceBuilder.ControllerFactory _controllerFactory;
    private readonly IServiceProvider _serviceProvider;

    public ControllerInstanceBuilder(
        IComponentRegistrar componentRegistrar,
        IControllerInstanceBuilder.ControllerFactory controllerFactory,
        IServiceProvider serviceProvider)
    {
        _componentRegistrar = componentRegistrar;
        _controllerFactory = controllerFactory;
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<ScopedResourceController> BuildControllers()
        => _componentRegistrar.ControllerRegistrations
            .Select(r => _controllerFactory.Invoke(_serviceProvider, r));

    public IEnumerable<ScopedResourceController> BuildControllers<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => _componentRegistrar.ControllerRegistrations
            .For<TEntity>()
            .Select(r => _controllerFactory.Invoke(_serviceProvider, r));
}
