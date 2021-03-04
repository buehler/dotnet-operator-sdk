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

        public IEnumerable<ControllerType> ControllerTypes => TypeCombo<ControllerType>(
            typeof(IResourceController<>),
            (t, tt) => new(t, tt));

        public IEnumerable<Type> FinalizerTypes => Types
            .Where(
                t => t.IsClass &&
                     !t.IsAbstract &&
                     t.GetInterfaces()
                         .Any(
                             i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceFinalizer<>)));

        public IEnumerable<ValidatorType> ValidatorTypes => TypeCombo<ValidatorType>(
            typeof(IValidationWebhook<>),
            (t, tt) => new(t, tt));

        public IEnumerable<MutatorType> MutatorTypes => TypeCombo<MutatorType>(
            typeof(IMutationWebhook<>),
            (t, tt) => new(t, tt));

        private IEnumerable<Type> Types => _assemblies
            .SelectMany(a => a.GetTypes())
            .Distinct(new TypeComparer());

        public AdditionalTypes Add(Assembly assembly)
        {
            var controller = ControllerTypes.ToList();
            var finalizer = FinalizerTypes.ToList();
            var validators = ValidatorTypes.ToList();
            var mutators = MutatorTypes.ToList();

            _assemblies.Add(assembly);

            return new AdditionalTypes(
                ControllerTypes.Except(controller).Select(c => c.Type),
                FinalizerTypes.Except(finalizer),
                ValidatorTypes.Except(validators).Select(v => v.Type),
                MutatorTypes.Except(mutators).Select(m => m.Type));
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

        private IEnumerable<TType> TypeCombo<TType>(Type genericType, Func<Type, Type, TType> ctor)
            => Types.Where(
                    t => t.IsClass &&
                         !t.IsAbstract &&
                         t.GetInterfaces()
                             .Any(
                                 i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType))
                .Select(
                    t => ctor(
                        t,
                        t.GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType)
                            .GenericTypeArguments[0]));

        internal record AdditionalTypes(
            IEnumerable<Type> Controllers,
            IEnumerable<Type> Finalizers,
            IEnumerable<Type> Validators,
            IEnumerable<Type> Mutators);

        internal record ControllerType(Type Type, Type ResourceType);

        internal record ValidatorType(Type Type, Type ResourceType);

        internal record MutatorType(Type Type, Type ResourceType);

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
