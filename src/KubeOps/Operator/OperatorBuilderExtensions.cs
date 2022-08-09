using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Webhooks;

namespace KubeOps.Operator;

public static class OperatorBuilderExtensions
{
    /// <summary>
    /// <para>
    /// Adds an controller to the operator and registers it to be used for all entities supported
    /// by its type definition.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <param name="builder">The builder (provided via extension instead of direct call).</param>
    /// <typeparam name="TImplementation">The type of the controller to register.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public static IOperatorBuilder AddController<TImplementation>(this IOperatorBuilder builder)
        where TImplementation : class
    {
        var entityTypes = typeof(TImplementation).GetInterfaces()
            .Where(
                t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IResourceController<>)))
            .Select(i => i.GenericTypeArguments[0]);

        var genericRegistrationMethod = builder
            .GetType()
            .GetMethods()
            .Single(m => m.Name == nameof(AddController) && m.GetGenericArguments().Length == 2);

        foreach (var entityType in entityTypes)
        {
            var registrationMethod =
                genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
            registrationMethod.Invoke(builder, Array.Empty<object>());
        }

        return builder;
    }

    /// <summary>
    /// <para>
    /// Adds a finalizer to the operator and registers it to be used for all entities supported
    /// by its type definition.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <param name="builder">The builder (provided via extension instead of direct call).</param>
    /// <typeparam name="TImplementation">The type of the finalizer to register.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public static IOperatorBuilder AddFinalizer<TImplementation>(this IOperatorBuilder builder)
        where TImplementation : class
    {
        var entityTypes = typeof(TImplementation).GetInterfaces()
            .Where(
                t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IResourceFinalizer<>)))
            .Select(i => i.GenericTypeArguments[0]);

        var genericRegistrationMethod = builder
            .GetType()
            .GetMethods()
            .Single(m => m.Name == nameof(AddFinalizer) && m.GetGenericArguments().Length == 2);

        foreach (var entityType in entityTypes)
        {
            var registrationMethod =
                genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
            registrationMethod.Invoke(builder, Array.Empty<object>());
        }

        return builder;
    }

    /// <summary>
    /// <para>
    /// Adds a validating webhook to the operator and registers it to be used for all entities
    /// supported by its type definition.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <param name="builder">The builder (provided via extension instead of direct call).</param>
    /// <typeparam name="TImplementation">The type of the webhook to register.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public static IOperatorBuilder AddValidationWebhook<TImplementation>(this IOperatorBuilder builder)
        where TImplementation : class
    {
        var entityTypes = typeof(TImplementation).GetInterfaces()
            .Where(
                t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IValidationWebhook<>)))
            .Select(i => i.GenericTypeArguments[0]);

        var genericRegistrationMethod = builder
            .GetType()
            .GetMethods()
            .Single(m => m.Name == nameof(AddValidationWebhook) && m.GetGenericArguments().Length == 2);

        foreach (var entityType in entityTypes)
        {
            var registrationMethod =
                genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
            registrationMethod.Invoke(builder, Array.Empty<object>());
        }

        return builder;
    }

    /// <summary>
    /// <para>
    /// Adds a mutating webhook to the operator and registers it to be used for all entities
    /// supported by its type definition.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <param name="builder">The builder (provided via extension instead of direct call).</param>
    /// <typeparam name="TImplementation">The type of the webhook to register.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public static IOperatorBuilder AddMutationWebhook<TImplementation>(this IOperatorBuilder builder)
        where TImplementation : class
    {
        var entityTypes = typeof(TImplementation).GetInterfaces()
            .Where(
                t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IMutationWebhook<>)))
            .Select(i => i.GenericTypeArguments[0]);

        var genericRegistrationMethod = builder
            .GetType()
            .GetMethods()
            .Single(m => m.Name == nameof(AddMutationWebhook) && m.GetGenericArguments().Length == 2);

        foreach (var entityType in entityTypes)
        {
            var registrationMethod =
                genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
            registrationMethod.Invoke(builder, Array.Empty<object>());
        }

        return builder;
    }

    // Yes, this is here for a reason. To avoid complexity in AssemblyScanner,
    // this class needed to have a method for AddEntity, even though it is a
    // private method that is a straight pass-through to the interface's method.
    internal static IOperatorBuilder AddEntity<TEntity>(this IOperatorBuilder builder)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => builder.AddEntity<TEntity>();
}
