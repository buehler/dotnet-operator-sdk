using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KubeOps.Operator
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddResourceController<TController>(this IServiceCollection services)
            where TController : class, IResourceController =>
            services.AddHostedService<TController>();

        public static IServiceCollection AddResourceController<TController, TEntity>(this IServiceCollection services)
            where TController : class, IResourceController<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta> =>
            services.AddHostedService<TController>();

        public static IServiceCollection AddResourceFinalizer<TFinalizer, TEntity>(this IServiceCollection services)
            where TFinalizer : class, IResourceFinalizer<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta> =>
            services.AddTransient<IResourceFinalizer<TEntity>, TFinalizer>();

        /// <summary>
        /// Adds an <see cref="IHealthCheck"/> to "both" healthy probes.
        /// The health check will be executed on the "/health" and "/ready" route.
        /// (The routes can be configured via operator settings).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">An optional name of the health check</param>
        /// <typeparam name="THealthCheck">The type that should be added to both probes.</typeparam>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddHealthCheck<THealthCheck>(
            this IServiceCollection services,
            string? name = default)
            where THealthCheck : class, IHealthCheck =>
            services
                .AddHealthChecks()
                .AddCheck<THealthCheck>(
                    name ?? typeof(THealthCheck).Name,
                    tags: new[] { OperatorStartup.ReadinessTag, OperatorStartup.LivenessTag })
                .Services;

        /// <summary>
        /// Adds an <see cref="IHealthCheck"/> to the readiness probe.
        /// The health check will be executed on the "/ready" route.
        /// (The route can be configured via operator settings).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">An optional name of the readiness check.</param>
        /// <typeparam name="TReadinessCheck">The type that should be added to the probe.</typeparam>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddReadinessCheck<TReadinessCheck>(
            this IServiceCollection services,
            string? name = default)
            where TReadinessCheck : class, IHealthCheck =>
            services
                .AddHealthChecks()
                .AddCheck<TReadinessCheck>(
                    name ?? typeof(TReadinessCheck).Name,
                    tags: new[] { OperatorStartup.ReadinessTag })
                .Services;

        /// <summary>
        /// Adds an <see cref="IHealthCheck"/> to the liveness probe.
        /// The health check will be executed on the "/health" route.
        /// (The route can be configured via operator settings).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">An optional name of the liveness check.</param>
        /// <typeparam name="TLivenessCheck">The type that should be added to the probe.</typeparam>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddLivenessCheck<TLivenessCheck>(
            this IServiceCollection services,
            string? name = default)
            where TLivenessCheck : class, IHealthCheck =>
            services
                .AddHealthChecks()
                .AddCheck<TLivenessCheck>(
                    name ?? typeof(TLivenessCheck).Name,
                    tags: new[] { OperatorStartup.LivenessTag })
                .Services;
    }
}
