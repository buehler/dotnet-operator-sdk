using System;
using System.Collections.Immutable;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Builder
{
    internal interface IComponentRegistrar
    {
        public ImmutableHashSet<EntityRegistration> EntityRegistrations { get; }

        public ImmutableHashSet<ControllerRegistration> ControllerRegistrations { get; }

        public ImmutableHashSet<FinalizerRegistration> FinalizerRegistrations { get; }

        public ImmutableHashSet<ValidatorRegistration> ValidatorRegistrations { get; }

        public ImmutableHashSet<MutatorRegistration> MutatorRegistrations { get; }

        IComponentRegistrar RegisterEntity<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>;

        IComponentRegistrar RegisterController<TController, TEntity>()
            where TController : class
            where TEntity : IKubernetesObject<V1ObjectMeta>;

        IComponentRegistrar RegisterFinalizer<TFinalizer, TEntity>()
            where TFinalizer : class
            where TEntity : IKubernetesObject<V1ObjectMeta>;

        IComponentRegistrar RegisterValidator<TValidator, TEntity>()
            where TValidator : class
            where TEntity : IKubernetesObject<V1ObjectMeta>;

        IComponentRegistrar RegisterMutator<TMutator, TEntity>()
            where TMutator : class
            where TEntity : IKubernetesObject<V1ObjectMeta>;

        public record EntityRegistration(Type EntityType);

        public record ControllerRegistration(Type ControllerType, Type EntityType);

        public record FinalizerRegistration(Type FinalizerType, Type EntityType);

        public record ValidatorRegistration(Type ValidatorType, Type EntityType);

        public record MutatorRegistration(Type MutatorType, Type EntityType);
    }
}
