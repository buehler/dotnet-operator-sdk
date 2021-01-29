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

        public ResourceLocator(params Assembly[] assemblies)
        {
            _assemblies = new HashSet<Assembly>(assemblies);
        }

        public IEnumerable<ControllerType> ControllerTypes => Types
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

        public IEnumerable<Type> FinalizerTypes => Types
            .Where(
                t => t.IsClass &&
                     !t.IsAbstract &&
                     t.GetInterfaces()
                         .Any(
                             i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceFinalizer<>)));

        public IEnumerable<ValidatorType> ValidatorTypes => Types
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

        private IEnumerable<Type> Types => _assemblies
            .SelectMany(a => a.GetTypes())
            .Distinct(new TypeComparer());

        public AdditionalTypes Add(Assembly assembly)
        {
            var controller = ControllerTypes.ToList();
            var finalizer = FinalizerTypes.ToList();
            var validator = ValidatorTypes.ToList();

            _assemblies.Add(assembly);

            return new AdditionalTypes(
                ControllerTypes.Except(controller).Select(c => c.Type),
                FinalizerTypes.Except(finalizer),
                ValidatorTypes.Except(validator).Select(v => v.Type));
        }

        public IEnumerable<Type> GetTypesWithAttribute<TAttribute>()
            where TAttribute : Attribute =>
            _assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.GetCustomAttributes<TAttribute>().Any());

        public IEnumerable<TAttribute> GetAttributes<TAttribute>()
            where TAttribute : Attribute =>
            _assemblies.SelectMany(
                    assembly => assembly.GetTypes())
                .SelectMany(type => type.GetCustomAttributes<TAttribute>(true));

        internal record AdditionalTypes(
            IEnumerable<Type> Controllers,
            IEnumerable<Type> Finalizers,
            IEnumerable<Type> Validators);

        internal record ControllerType(Type Type, Type ResourceType);

        internal record ValidatorType(Type Type, Type ResourceType);

        private class TypeComparer : IEqualityComparer<Type?>
        {
            public bool Equals(Type? x, Type? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return false;
                }

                return x.GUID.Equals(y.GUID);
            }

            public int GetHashCode(Type obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
