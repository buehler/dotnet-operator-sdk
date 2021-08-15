using System;
using System.Collections.Generic;
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
using KubeOps.Operator.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Prometheus;
using YamlDotNet.Serialization;

namespace KubeOps.Operator.Builder
{
    internal class OperatorBuilder : IOperatorBuilder
    {
        internal const string LivenessTag = "liveness";
        internal const string ReadinessTag = "readiness";

        private readonly ResourceLocator _resourceLocator = new(
            Assembly.GetEntryAssembly() ?? throw new Exception("No Entry Assembly found."),
            Assembly.GetExecutingAssembly());

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
            // This is kind of ugly to register the newly found controllers and stuff.
            // Maybe this needs to be refactored.
            var (controllers, finalizers, validators, mutators) = _resourceLocator.Add(assembly);

            foreach (var controllerType in controllers)
            {
                Services.TryAddScoped(controllerType);
            }

            foreach (var finalizerType in finalizers)
            {
                Services.TryAddScoped(finalizerType);
            }

            foreach (var validatorType in validators)
            {
                Services.TryAddScoped(validatorType);
            }

            foreach (var mutatorType in mutators)
            {
                Services.TryAddScoped(mutatorType);
            }

            return this;
        }

        public IOperatorBuilder AddController<TImplementation>()
            where TImplementation : class
        {
            Services.TryAddScoped<TImplementation>();

            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IResourceController<>)))
                .Select(i => i.GenericTypeArguments[0]);

            foreach (var entityType in entityTypes)
            {
                Services.AddSingleton(new ControllerType(typeof(TImplementation), entityType));

                var controllerInstanceImplType = typeof(ControllerType<>).MakeGenericType(entityType);

                var controllerInstanceConstructor =
                    controllerInstanceImplType.GetConstructor(
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new[] { typeof(Type) },
                        null);

                if (controllerInstanceConstructor is null)
                {
                    continue; // This should never happen, but it gets the compiler to shut up about a possible null dereference in the below factory method.
                }

                Services.AddSingleton(
                    controllerInstanceImplType,
                    controllerInstanceConstructor.Invoke(new object[] { typeof(TImplementation) }));
            }

            return this;
        }

        public IOperatorBuilder AddController<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
            Services.AddSingleton(new ControllerType(typeof(TImplementation), typeof(TEntity)));
            Services.AddSingleton(new ControllerType<TEntity>(typeof(TImplementation)));

            return this;
        }

        public IOperatorBuilder AddFinalizer<TImplementation>()
            where TImplementation : class
        {
            Services.TryAddScoped<TImplementation>();

            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IResourceFinalizer<>)))
                .Select(i => i.GenericTypeArguments[0]);

            foreach (var entityType in entityTypes)
            {
                Services.AddSingleton(new FinalizerType(typeof(TImplementation), entityType));

                var finalizerInstanceImplType = typeof(FinalizerType<>).MakeGenericType(entityType);

                var finalizerInstanceConstructor =
                    finalizerInstanceImplType.GetConstructor(
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new[] { typeof(Type) },
                        null);

                if (finalizerInstanceConstructor is null)
                {
                    continue; // This should never happen, but it gets the compiler to shut up about a possible null dereference in the below factory method.
                }

                Services.AddSingleton(
                    finalizerInstanceImplType,
                    finalizerInstanceConstructor.Invoke(new object[] { typeof(TImplementation) }));
            }

            return this;
        }

        public IOperatorBuilder AddFinalizer<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
            Services.AddSingleton(new FinalizerType(typeof(TImplementation), typeof(TEntity)));
            Services.AddSingleton(new FinalizerType<TEntity>(typeof(TImplementation)));

            return this;
        }

        public IOperatorBuilder AddValidationWebhook<TImplementation>()
            where TImplementation : class
        {
            Services.TryAddScoped<TImplementation>();

            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IValidationWebhook<>)))
                .Select(i => i.GenericTypeArguments[0]);

            foreach (var entityType in entityTypes)
            {
                Services.AddSingleton(new ValidatorType(typeof(TImplementation), entityType));

                var validatorInstanceImplType = typeof(ValidatorType<>).MakeGenericType(entityType);

                var validatorInstanceConstructor =
                    validatorInstanceImplType.GetConstructor(
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new[] { typeof(Type) },
                        null);

                if (validatorInstanceConstructor is null)
                {
                    continue; // This should never happen, but it gets the compiler to shut up about a possible null dereference in the below factory method.
                }

                Services.AddSingleton(
                    validatorInstanceImplType,
                    validatorInstanceConstructor.Invoke(new object[] { typeof(TImplementation) }));
            }

            return this;
        }

        public IOperatorBuilder AddValidationWebhook<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
            Services.AddSingleton(new ValidatorType(typeof(TImplementation), typeof(TEntity)));
            Services.AddSingleton(new ValidatorType<TEntity>(typeof(TImplementation)));

            return this;
        }

        public IOperatorBuilder AddMutationWebhook<TImplementation>()
            where TImplementation : class
        {
            Services.TryAddScoped<TImplementation>();

            var entityTypes = typeof(TImplementation).GetInterfaces().Where(t =>
                    t.IsConstructedGenericType &&
                    t.GetGenericTypeDefinition().IsEquivalentTo(typeof(IMutationWebhook<>)))
                .Select(i => i.GenericTypeArguments[0]);

            foreach (var entityType in entityTypes)
            {
                Services.AddSingleton(new MutatorType(typeof(TImplementation), entityType));

                var mutatorInstanceImplType = typeof(MutatorType<>).MakeGenericType(entityType);

                var mutatorInstanceConstructor =
                    mutatorInstanceImplType.GetConstructor(
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new[] { typeof(Type) },
                        null);

                if (mutatorInstanceConstructor is null)
                {
                    continue; // This should never happen, but it gets the compiler to shut up about a possible null dereference in the below factory method.
                }

                Services.AddSingleton(
                    mutatorInstanceImplType,
                    mutatorInstanceConstructor.Invoke(new object[] { typeof(TImplementation) }));
            }

            return this;
        }

        public IOperatorBuilder AddMutationWebhook<TImplementation, TEntity>()
            where TImplementation : class
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            Services.TryAddScoped<TImplementation>();
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

            Services.AddSingleton(_resourceLocator);

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

            // Add  all found controller types.
            Services.AddHostedService<ResourceControllerManager>();

            // TODO Assembly Searching
            /*foreach (var (controllerType, _) in _resourceLocator.ControllerTypes)
            {
                Services.TryAddScoped(controllerType);
            }*/

            // Register all found finalizer for the finalize manager
            Services.AddTransient(typeof(IFinalizerManager<>), typeof(FinalizerManager<>));

            // TODO Assembly Searching
            /*foreach (var finalizerType in _resourceLocator.FinalizerTypes)
            {
                Services.TryAddScoped(finalizerType);
            }*/

            // TODO Assembly Searching
            // Register all found validation webhooks
            /*foreach (var (validatorType, _) in _resourceLocator.ValidatorTypes)
            {
                Services.TryAddScoped(validatorType);
            }*/

            // TODO Assembly Searching
            // Register all found mutation webhooks
            /*foreach (var (mutatorType, _) in _resourceLocator.MutatorTypes)
            {
                Services.TryAddScoped(mutatorType);
            }*/

            return this;
        }
    }
}
