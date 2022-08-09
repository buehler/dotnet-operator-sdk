using System.Collections.Immutable;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Webhooks;
using static KubeOps.Operator.Builder.IComponentRegistrar;

namespace KubeOps.Operator.Builder;

internal class ComponentRegistrar : IComponentRegistrar
{
    private readonly HashSet<EntityRegistration> _entityRegistrations = new();
    private readonly HashSet<ControllerRegistration> _controllerRegistrations = new();
    private readonly HashSet<FinalizerRegistration> _finalizerRegistrations = new();
    private readonly HashSet<ValidatorRegistration> _validatorRegistrations = new();
    private readonly HashSet<MutatorRegistration> _mutatorRegistrations = new();

    public ImmutableHashSet<EntityRegistration> EntityRegistrations => _entityRegistrations.ToImmutableHashSet();

    public ImmutableHashSet<ControllerRegistration> ControllerRegistrations =>
        _controllerRegistrations.ToImmutableHashSet();

    public ImmutableHashSet<FinalizerRegistration> FinalizerRegistrations =>
        _finalizerRegistrations.ToImmutableHashSet();

    public ImmutableHashSet<ValidatorRegistration> ValidatorRegistrations =>
        _validatorRegistrations.ToImmutableHashSet();

    public ImmutableHashSet<MutatorRegistration> MutatorRegistrations => _mutatorRegistrations.ToImmutableHashSet();

    public IComponentRegistrar RegisterEntity<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _entityRegistrations.Add(new EntityRegistration(typeof(TEntity)));

        return this;
    }

    public IComponentRegistrar RegisterController<TController, TEntity>()
        where TController : class, IResourceController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _controllerRegistrations.Add(new ControllerRegistration(typeof(TController), typeof(TEntity)));

        return RegisterEntity<TEntity>();
    }

    public IComponentRegistrar RegisterFinalizer<TFinalizer, TEntity>()
        where TFinalizer : class, IResourceFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _finalizerRegistrations.Add(new FinalizerRegistration(typeof(TFinalizer), typeof(TEntity)));

        return this;
    }

    public IComponentRegistrar RegisterValidator<TValidator, TEntity>()
        where TValidator : class, IValidationWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _validatorRegistrations.Add(new ValidatorRegistration(typeof(TValidator), typeof(TEntity)));

        return this;
    }

    public IComponentRegistrar RegisterMutator<TMutator, TEntity>()
        where TMutator : class, IMutationWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _mutatorRegistrations.Add(new MutatorRegistration(typeof(TMutator), typeof(TEntity)));

        return RegisterEntity<TEntity>();
    }
}
