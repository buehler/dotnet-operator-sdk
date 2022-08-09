using System.Collections.Immutable;
using static KubeOps.Operator.Builder.IComponentRegistrar;

namespace KubeOps.Operator;

internal static class HashSetExtensions
{
    public static IEnumerable<ControllerRegistration> For<TEntity>(
        this ImmutableHashSet<ControllerRegistration> registrations)
        => registrations.Where(r => r.EntityType.IsEquivalentTo(typeof(TEntity)));

    public static IEnumerable<FinalizerRegistration> For<TEntity>(
        this ImmutableHashSet<FinalizerRegistration> registrations)
        => registrations.Where(r => r.EntityType.IsEquivalentTo(typeof(TEntity)));

    public static IEnumerable<ValidatorRegistration> For<TEntity>(
        this ImmutableHashSet<ValidatorRegistration> registrations)
        => registrations.Where(r => r.EntityType.IsEquivalentTo(typeof(TEntity)));

    public static IEnumerable<MutatorRegistration> For<TEntity>(
        this ImmutableHashSet<MutatorRegistration> registrations)
        => registrations.Where(r => r.EntityType.IsEquivalentTo(typeof(TEntity)));
}
