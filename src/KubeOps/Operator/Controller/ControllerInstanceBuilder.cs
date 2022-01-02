using System;
using System.Collections.Generic;
using System.Linq;
using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;
using static KubeOps.Operator.Builder.IComponentRegistrar;

namespace KubeOps.Operator.Controller;

internal class ControllerInstanceBuilder : IControllerInstanceBuilder
{
    private readonly IComponentRegistrar _componentRegistrar;
    private readonly Func<ControllerRegistration, IManagedResourceController> _controllerFactory;

    public ControllerInstanceBuilder(
        IComponentRegistrar componentRegistrar,
        Func<ControllerRegistration, IManagedResourceController> controllerFactory)
    {
        _componentRegistrar = componentRegistrar;
        _controllerFactory = controllerFactory;
    }

    public IEnumerable<IManagedResourceController> BuildControllers()
        => _componentRegistrar.ControllerRegistrations
            .Select(_controllerFactory);

    public IEnumerable<IManagedResourceController> BuildControllers<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => _componentRegistrar.ControllerRegistrations
            .For<TEntity>()
            .Select(_controllerFactory);
}
