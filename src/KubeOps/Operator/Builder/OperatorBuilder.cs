using System;
using System.Collections.Generic;
using System.Reflection;
using DotnetKubernetesClient;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Controller;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Leadership;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Serialization;
using KubeOps.Operator.Services;
using KubeOps.Operator.Watcher;
using Microsoft.Extensions.DependencyInjection;
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

        public OperatorBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
        
        private IResourceTypeService ResourceTypeService { get; set; }

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

        public IOperatorBuilder AddController<TController>()
            where TController : class, IResourceController
        {
            Services.AddHostedService<TController>();

            return this;
        }

        public IOperatorBuilder AddFinalizer<TFinalizer>()
            where TFinalizer : class, IResourceFinalizer
        {
            Services.AddTransient(typeof(IResourceFinalizer), typeof(TFinalizer));

            return this;
        }

        public IOperatorBuilder AddResourceAssembly(Assembly assembly)
        {
            ResourceTypeService.AddAssembly(assembly);

            return this;
        }

        internal IOperatorBuilder AddOperatorBase(OperatorSettings settings)
        {
            Services.AddSingleton(settings);

            // support lazy service resolution
            Services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

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
                    .WithTypeConverter(new YamlIntOrStrTypeConverter())
                    .Build());

            Services.AddTransient<EntitySerializer>();

            Services.AddTransient<IKubernetesClient, KubernetesClient>();

            Services.AddTransient(typeof(IResourceCache<>), typeof(ResourceCache<>));
            Services.AddTransient(typeof(IResourceWatcher<>), typeof(ResourceWatcher<>));
            Services.AddTransient(typeof(IResourceEventQueue<>), typeof(ResourceEventQueue<>));
            Services.AddTransient(typeof(IResourceServices<>), typeof(ResourceServices<>));

            // Support for healthchecks and prometheus.
            Services
                .AddHealthChecks()
                .ForwardToPrometheus();

            // Add the default controller liveness check.
            AddHealthCheck<ControllerLivenessCheck>();

            // Support for leader election via V1Leases.
            Services.AddHostedService<LeaderElector>();
            Services.AddSingleton<ILeaderElection, LeaderElection>();

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                throw new Exception("No Entry Assembly found.");
            }

            ResourceTypeService = new ResourceTypeService(entryAssembly, Assembly.GetExecutingAssembly());

            Services.AddSingleton(ResourceTypeService);

            return this;
        }
    }
}
