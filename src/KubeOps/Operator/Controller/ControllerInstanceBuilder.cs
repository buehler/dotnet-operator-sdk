using System;
using System.Collections.Generic;
using System.Linq;
using KubeOps.Operator.Builder;
using Microsoft.Extensions.DependencyInjection;
using static KubeOps.Operator.Builder.IComponentRegistrar;

namespace KubeOps.Operator.Controller
{
    internal class ControllerInstanceBuilder : IControllerInstanceBuilder
    {
        private readonly IComponentRegistrar _componentRegistrar;
        private readonly Func<ControllerRegistration, IManagedResourceController> _factoryFunction;

        public ControllerInstanceBuilder(IComponentRegistrar componentRegistrar, IServiceProvider services)
        {
            _componentRegistrar = componentRegistrar;

            _factoryFunction = r =>
            {
                var managedControllerType = typeof(ManagedResourceController<>).MakeGenericType(r.EntityType);
                return (IManagedResourceController)ActivatorUtilities.CreateInstance(
                    services,
                    managedControllerType,
                    r);
            };
        }

        public IEnumerable<IManagedResourceController> MakeManagedControllers()
            => _componentRegistrar.ControllerRegistrations
                .Select(_factoryFunction);
    }
}
