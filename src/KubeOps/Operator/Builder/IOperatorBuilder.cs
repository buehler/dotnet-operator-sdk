using System.Reflection;
using k8s;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KubeOps.Operator.Builder
{
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
        /// Adds a resource controller for an entity of <see cref="IKubernetesObject{TMetadata}"/>.
        /// This controller is taking care for the reconciliation of that particular entity.
        /// </summary>
        /// <typeparam name="TController">The type of the controller to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IOperatorBuilder AddController<TController>()
            where TController : class, IResourceController;

        /// <summary>
        /// Adds a resource finalizer. Finalizers take care of "to be deleted" instances of
        /// an entity that needs to asynchronously perfrom some tasks.
        /// </summary>
        /// <typeparam name="TFinalizer">The type of the finalizer to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IOperatorBuilder AddFinalizer<TFinalizer>()
            where TFinalizer : class, IResourceFinalizer;

        /// <summary>
        /// Adds an assembly to the resource search path. This allows the given Assembly to searched
        /// for resources when generating CRDs or RBAC definitions.
        /// </summary>
        /// <param name="assembly">The assembly to add.</param>
        /// <returns>The builder for chaining.</returns>
        IOperatorBuilder AddResourceAssembly(Assembly assembly);
    }
}
