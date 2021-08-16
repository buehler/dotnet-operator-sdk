using System;
using System.Collections.Generic;
using System.Linq;
using KubeOps.Operator.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Controller
{
    internal class ControllerInstanceBuilder : IControllerInstanceBuilder
    {
        private readonly IComponentRegistrar _componentRegistrar;
        private readonly IServiceProvider _services;

        public ControllerInstanceBuilder(IComponentRegistrar componentRegistrar, IServiceProvider services)
        {
            _componentRegistrar = componentRegistrar;
            _services = services;
        }

        public IEnumerable<IManagedResourceController> MakeManagedControllers()
            => _componentRegistrar.ControllerRegistrations
                .Select(
                    r =>
                    {
                        var managedControllerType = typeof(ManagedResourceController<>).MakeGenericType(r.EntityType);
                        var managedControllerInstance =
                            _services.GetRequiredService(managedControllerType) as IManagedResourceController ??
                            throw new Exception(
                                $"Could not create managed controller with type {managedControllerType}.");

                        // TODO This is an anti-pattern, but it's how it was already done and writing up a factory method will have to wait.
                        managedControllerInstance.ControllerType = r.ControllerType;
                        return managedControllerInstance;
                    });
    }
}
