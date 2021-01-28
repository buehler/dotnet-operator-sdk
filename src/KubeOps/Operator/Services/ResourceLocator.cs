using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Webhooks;

namespace KubeOps.Operator.Services
{
    internal class ResourceLocator
    {
        private readonly ICollection<Assembly> _assemblies;

        public ResourceLocator(IEnumerable<Assembly> assemblies)
        {
            _assemblies = new HashSet<Assembly>(assemblies);
        }

        public IEnumerable<ControllerType> ControllerTypes => _assemblies
            .SelectMany(a => a.GetTypes())
            .Where(
                t => t.IsClass &&
                     !t.IsAbstract &&
                     t.GetInterfaces()
                         .Any(
                             i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceController<>)))
            .Select(
                t => new ControllerType(
                    t,
                    t.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceController<>))
                        .GenericTypeArguments[0]));

        public IEnumerable<Type> FinalizerTypes => _assemblies
            .SelectMany(a => a.GetTypes())
            .Where(
                t => t.IsClass &&
                     !t.IsAbstract &&
                     t.GetInterfaces()
                         .Any(
                             i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceFinalizer<>)));

        public IEnumerable<ValidatorType> ValidatorTypes => _assemblies
            .SelectMany(a => a.GetTypes())
            .Where(
                t => t.IsClass &&
                     !t.IsAbstract &&
                     t.GetInterfaces()
                         .Any(
                             i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationWebhook<>)))
            .Select(
                t => new ValidatorType(
                    t,
                    t.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationWebhook<>))
                        .GenericTypeArguments[0]));

        public void Add(Assembly assembly) => _assemblies.Add(assembly);

        public IEnumerable<Type> GetTypesWithAttribute<TAttribute>()
            where TAttribute : Attribute =>
            _assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes<TAttribute>().Any());

        public IEnumerable<TAttribute> GetAttributes<TAttribute>()
            where TAttribute : Attribute =>
            _assemblies.SelectMany(
                    assembly => assembly.GetTypes())
                .SelectMany(type => type.GetCustomAttributes<TAttribute>(true));

        internal record ControllerType(Type Type, Type ResourceType);

        internal record ValidatorType(Type Type, Type ResourceType);
    }
}
