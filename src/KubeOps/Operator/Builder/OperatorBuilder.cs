﻿using System;
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

        internal static readonly Assembly[] Assemblies =
        {
            Assembly.GetEntryAssembly() ?? throw new Exception("No Entry Assembly found."),
            Assembly.GetExecutingAssembly(),
        };

        private readonly IResourceTypeService _resourceTypeService;

        public OperatorBuilder(IServiceCollection services)
        {
            Services = services;
            _resourceTypeService = new ResourceTypeService(Assemblies);
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

        public IOperatorBuilder AddFinalizer<TFinalizer>()
            where TFinalizer : class, IResourceFinalizer
        {
            Services.AddTransient(typeof(IResourceFinalizer), typeof(TFinalizer));

            return this;
        }

        public IOperatorBuilder AddResourceAssembly(Assembly assembly)
        {
            _resourceTypeService.AddAssembly(assembly);

            return this;
        }

        public IOperatorBuilder AddValidationWebhook<TWebhook>()
            where TWebhook : class, IValidationWebhook
        {
            Services.AddTransient(typeof(IValidationWebhook), typeof(TWebhook));
            Services.AddTransient(typeof(TWebhook));

            return this;
        }

        internal static IEnumerable<(Type ControllerType, Type EntityType)> GetControllers() => Assemblies
            .SelectMany(a => a.GetTypes())
            .Where(
                t => t.IsClass &&
                     !t.IsAbstract &&
                     t.GetInterfaces()
                         .Any(
                             i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceController<>)))
            .Select(
                t => (t,
                    t.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceController<>))
                        .GenericTypeArguments[0]));

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

            Services.AddSingleton(_resourceTypeService);
            Services.AddHostedService<ResourceControllerManager>();

            // Add the service provider (for instantiation)
            // and all found controller types.
            Services.TryAddSingleton(sp => sp);
            foreach (var (controllerType, _) in GetControllers())
            {
                Services.TryAddScoped(controllerType);
            }

            return this;
        }
    }
}
