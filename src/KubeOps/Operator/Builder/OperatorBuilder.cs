using System;
using System.Linq;
using System.Reflection;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Controller;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Events;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Kubernetes;
using KubeOps.Operator.Leadership;
using KubeOps.Operator.Serialization;
using KubeOps.Operator.Services;
using KubeOps.Operator.Util;
using KubeOps.Operator.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using YamlDotNet.Serialization;

namespace KubeOps.Operator.Builder
{
    internal class OperatorBuilder : IOperatorBuilder
    {
        internal const string LivenessTag = "liveness";
        internal const string ReadinessTag = "readiness";

        public OperatorBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public IOperatorBuilder AddHealthCheck<THealthCheck>(string? name = default)
            where THealthCheck : class, IHealthCheck
        {
            Services
                .AddHealthChecks()
                .AddCheck<THealthCheck>(
                    name ?? typeof(THealthCheck).Name,
                    tags: new[] { ReadinessTag, LivenessTag });

            return this;
        }

        public IOperatorBuilder AddReadinessCheck<TReadinessCheck>(string? name = default)
            where TReadinessCheck : class, IHealthCheck
        {
            Services
                .AddHealthChecks()
                .AddCheck<TReadinessCheck>(
                    name ?? typeof(TReadinessCheck).Name,
                    tags: new[] { ReadinessTag });

            return this;
        }

        public IOperatorBuilder AddLivenessCheck<TLivenessCheck>(string? name = default)
            where TLivenessCheck : class, IHealthCheck
        {
            Services
                .AddHealthChecks()
                .AddCheck<TLivenessCheck>(
                    name ?? typeof(TLivenessCheck).Name,
                    tags: new[] { LivenessTag });

            return this;
        }

        public IOperatorBuilder AddResourceAssembly(Assembly assembly)
        {
            // TODO Implement
            // throw new NotImplementedException();
            return this;
        }

        public IOperatorBuilder AddEntity<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.AddSingleton(new EntityType(typeof(TEntity)));

            return this;
        }

        public IOperatorBuilder AddController<TImplementation>()
            where TImplementation : class
        {
            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IResourceController<>)))
                .Select(i => i.GenericTypeArguments[0]);

            var genericRegistrationMethod = GetType()
                .GetMethods()
                .Single(m => m.Name == nameof(AddController) && m.GetGenericArguments().Length == 2);

            foreach (var entityType in entityTypes)
            {
                var registrationMethod =
                    genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
                registrationMethod.Invoke(this, Array.Empty<object>());
            }

            return this;
        }

        public IOperatorBuilder AddController<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
            Services.AddSingleton(new EntityType(typeof(TEntity)));
            Services.AddSingleton(new ControllerType(typeof(TImplementation), typeof(TEntity)));
            Services.AddSingleton(new ControllerType<TEntity>(typeof(TImplementation)));

            return this;
        }

        public IOperatorBuilder AddFinalizer<TImplementation>()
            where TImplementation : class
        {
            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IResourceFinalizer<>)))
                .Select(i => i.GenericTypeArguments[0]);

            var genericRegistrationMethod = GetType()
                .GetMethods()
                .Single(m => m.Name == nameof(AddFinalizer) && m.GetGenericArguments().Length == 2);

            foreach (var entityType in entityTypes)
            {
                var registrationMethod =
                    genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
                registrationMethod.Invoke(this, Array.Empty<object>());
            }

            return this;
        }

        public IOperatorBuilder AddFinalizer<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
            Services.AddSingleton(new EntityType(typeof(TEntity)));
            Services.AddSingleton(new FinalizerType(typeof(TImplementation), typeof(TEntity)));
            Services.AddSingleton(new FinalizerType<TEntity>(typeof(TImplementation)));

            return this;
        }

        public IOperatorBuilder AddValidationWebhook<TImplementation>()
            where TImplementation : class
        {
            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IValidationWebhook<>)))
                .Select(i => i.GenericTypeArguments[0]);

            var genericRegistrationMethod = GetType()
                .GetMethods()
                .Single(m => m.Name == nameof(AddValidationWebhook) && m.GetGenericArguments().Length == 2);

            foreach (var entityType in entityTypes)
            {
                var registrationMethod =
                    genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
                registrationMethod.Invoke(this, Array.Empty<object>());
            }

            return this;
        }

        public IOperatorBuilder AddValidationWebhook<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
            Services.AddSingleton(new EntityType(typeof(TEntity)));
            Services.AddSingleton(new ValidatorType(typeof(TImplementation), typeof(TEntity)));
            Services.AddSingleton(new ValidatorType<TEntity>(typeof(TImplementation)));

            return this;
        }

        public IOperatorBuilder AddMutationWebhook<TImplementation>()
            where TImplementation : class
        {
            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IMutationWebhook<>)))
                .Select(i => i.GenericTypeArguments[0]);

            var genericRegistrationMethod = GetType()
                .GetMethods()
                .Single(m => m.Name == nameof(AddMutationWebhook) && m.GetGenericArguments().Length == 2);

            foreach (var entityType in entityTypes)
            {
                var registrationMethod =
                    genericRegistrationMethod.MakeGenericMethod(typeof(TImplementation), entityType);
                registrationMethod.Invoke(this, Array.Empty<object>());
            }

            return this;
        }

        public IOperatorBuilder AddMutationWebhook<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
            Services.AddSingleton(new EntityType(typeof(TEntity)));
            Services.AddSingleton(new MutatorType(typeof(TImplementation), typeof(TEntity)));
            Services.AddSingleton(new MutatorType<TEntity>(typeof(TImplementation)));

            return this;
        }

        internal IOperatorBuilder AddOperatorBase(OperatorSettings settings)
        {
            Services.AddSingleton(settings);

            Services.AddTransient(
                _ => new SerializerBuilder()
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .WithNamingConvention(new NamingConvention())
                    .WithTypeConverter(new Yaml.ByteArrayStringYamlConverter())
                    .WithTypeConverter(new IntOrStringYamlConverter())
                    .Build());

            Services.AddTransient<EntitySerializer>();

            Services.AddTransient<IKubernetesClient, KubernetesClient>();
            Services.AddTransient<IEventManager, EventManager>();

            Services.AddTransient(typeof(ResourceCache<>));
            Services.AddTransient(typeof(ResourceWatcher<>));
            Services.AddTransient(typeof(ManagedResourceController<>));

            // Support all the metrics
            Services.AddSingleton(typeof(ResourceWatcherMetrics<>));
            Services.AddSingleton(typeof(ResourceCacheMetrics<>));
            Services.AddSingleton(typeof(ResourceControllerMetrics<>));

            // Support for healthchecks and prometheus.
            Services
                .AddHealthChecks()
                .ForwardToPrometheus();

            // Support for leader election via V1Leases.
            Services.AddHostedService<LeaderElector>();
            Services.AddSingleton<ILeaderElection, LeaderElection>();

            // Register event handler
            Services.AddTransient<IEventManager, EventManager>();

            // Register controller manager
            Services.AddHostedService<ResourceControllerManager>();

            // Register finalizer manager
            Services.AddTransient(typeof(IFinalizerManager<>), typeof(FinalizerManager<>));

            // Register builders for RBAC rules and CRDs
            Services.TryAddSingleton<ICrdBuilder, CrdBuilder>();
            Services.TryAddSingleton<IRbacBuilder, RbacBuilder>();

            // TODO Assembly Searching for Controllers
            // TODO Assembly Searching for Finalizers
            // TODO Assembly Searching for Validators
            // TODO Assembly Searching for Mutators
            return this;
        }
    }
}
