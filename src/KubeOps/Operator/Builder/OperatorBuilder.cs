using System;
using System.Collections.Generic;
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
            new[]
            {
                Assembly.GetEntryAssembly() ?? throw new Exception("No Entry Assembly found."),
                Assembly.GetExecutingAssembly(),
            });

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
            _resourceLocator.Add(assembly);
            return this;
        }

        internal IOperatorBuilder AddOperatorBase(OperatorSettings settings)
        {
            Services.AddSingleton(settings);

            var jsonSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new NamingConvention(),
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter { CamelCaseText = true },
                    new Iso8601TimeSpanConverter(),
                },
                DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffK",
            };
            Services.AddTransient(_ => jsonSettings);
            JsonConvert.DefaultSettings = () => jsonSettings;

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

            // Add the service provider (for instantiation)
            // and all found controller types.
            Services.AddHostedService<ResourceControllerManager>();
            Services.TryAddSingleton(sp => sp);
            foreach (var (controllerType, _) in _resourceLocator.ControllerTypes)
            {
                Services.TryAddScoped(controllerType);
            }

            // Register all found finalizer for the finalize manager
            Services.AddTransient(typeof(IFinalizerManager<>), typeof(FinalizerManager<>));
            foreach (var finalizerType in _resourceLocator.FinalizerTypes)
            {
                Services.TryAddScoped(finalizerType);
            }

            // Register all found validation webhooks
            foreach (var (validatorType, _) in _resourceLocator.ValidatorTypes)
            {
                Services.TryAddScoped(validatorType);
            }

            return this;
        }
    }
}
