using System.Reflection;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Webhooks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KubeOps.Operator.Builder;

/// <summary>
/// Builder for specific services of the kubernetes operator.
/// </summary>
public interface IOperatorBuilder
{
    /// <summary>
    /// Returns the original service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds an <see cref="IHealthCheck"/> to "both" healthy probes.
    /// The health check will be executed on the "/health" and "/ready" route.
    /// (The routes can be configured via operator settings).
    /// </summary>
    /// <param name="name">An optional name of the health check.</param>
    /// <typeparam name="THealthCheck">The type that should be added to both probes.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddHealthCheck<THealthCheck>(string? name = default)
        where THealthCheck : class, IHealthCheck;

    /// <summary>
    /// Adds an <see cref="IHealthCheck"/> to the readiness probe.
    /// The health check will be executed on the "/ready" route.
    /// (The route can be configured via operator settings).
    /// </summary>
    /// <param name="name">An optional name of the readiness check.</param>
    /// <typeparam name="TReadinessCheck">The type that should be added to the probe.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddReadinessCheck<TReadinessCheck>(string? name = default)
        where TReadinessCheck : class, IHealthCheck;

    /// <summary>
    /// Adds an <see cref="IHealthCheck"/> to the liveness probe.
    /// The health check will be executed on the "/health" route.
    /// (The route can be configured via operator settings).
    /// </summary>
    /// <param name="name">An optional name of the liveness check.</param>
    /// <typeparam name="TLivenessCheck">The type that should be added to the probe.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddLivenessCheck<TLivenessCheck>(string? name = default)
        where TLivenessCheck : class, IHealthCheck;

    /// <summary>
    /// <para>
    /// Adds an assembly to the resource search path. This allows the given Assembly to be searched
    /// for resources when generating CRDs or RBAC definitions.
    /// </para>
    /// <para>Also webhooks / controllers and finalizers will be searched in this assembly.</para>
    /// </summary>
    /// <param name="assembly">The assembly to add.</param>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddResourceAssembly(Assembly assembly);

    /// <summary>
    /// <para>
    /// Adds an entity to the operator to be considered for RBAC / CRD generation.
    /// </para>
    /// <para>
    /// Only useful if a) the given type is not referenced by a controller, finalizer, or webhook
    /// and b) the assembly containing the type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to register.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddEntity<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// <para>
    /// Adds an controller to the operator and registers it to be used for one specific entity type.
    /// Can be called multiple times for the same controller.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <typeparam name="TImplementation">The type of the controller to register.</typeparam>
    /// <typeparam name="TEntity">The type of the entity to associate the controller with.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddController<TImplementation, TEntity>()
        where TImplementation : class, IResourceController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// <para>
    /// Adds a finalizer to the operator and registers it to be used for one specific entity type.
    /// Can be called multiple times for the same finalizer.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <typeparam name="TImplementation">The type of the finalizer to register.</typeparam>
    /// <typeparam name="TEntity">The type of the entity to associate the finalizer with.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddFinalizer<TImplementation, TEntity>()
        where TImplementation : class, IResourceFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// <para>
    /// Adds a validating webhook to the operator and registers it to be used for one specific
    /// entity type. Can be called multiple times for the same webhook.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <typeparam name="TImplementation">The type of the webhook to register.</typeparam>
    /// <typeparam name="TEntity">The type of the entity to associate the webhook with.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddValidationWebhook<TImplementation, TEntity>()
        where TImplementation : class, IValidationWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// <para>
    /// Adds a mutating webhook to the operator and registers it to be used for one specific
    /// entity type. Can be called multiple times for the same webhook.
    /// </para>
    /// <para>
    /// Only useful if the assembly containing the given type is not already automatically scanned.
    /// </para>
    /// </summary>
    /// <typeparam name="TImplementation">The type of the webhook to register.</typeparam>
    /// <typeparam name="TEntity">The type of the entity to associate the webhook with.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddMutationWebhook<TImplementation, TEntity>()
        where TImplementation : class, IMutationWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// <para>
    /// Adds a hosted service to the system that creates a "localtunnel"
    /// (http://localtunnel.github.io/www/) to the running application.
    /// The tunnel points to the configured host/port configuration and then
    /// registers itself as webhook target within Kubernetes. This
    /// enables developers to easily create webhooks without the requirement
    /// of registering ngrok / localtunnel urls themselves.
    /// </para>
    /// <para>
    /// This is a convenience method to improve the developer experience.
    /// Since some IDEs do not gracefully shutdown applications that
    /// have a debugger attached, the registration may not be removed.
    /// </para>
    /// <para>
    /// It is strongly recommended to use this method only while developing
    /// or debugging an operator. *Never* use this in production.
    /// </para>
    /// </summary>
    /// <param name="hostname">The hostname that the tunnel should target to proxy.</param>
    /// <param name="port">The target port to proxy.</param>
    /// <param name="isHttps">If set to true, the target uses HTTPS.</param>
    /// <param name="allowUntrustedCertificates">
    /// If the target uses HTTPS, should self signed / untrusted certificates be allowed or not.
    /// </param>
    /// <returns>The builder for chaining.</returns>
    IOperatorBuilder AddWebhookLocaltunnel(
        string hostname = "localhost",
        short port = 5000,
        bool isHttps = false,
        bool allowUntrustedCertificates = true);
}
