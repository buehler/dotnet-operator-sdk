using System;
using System.Collections.Generic;
using k8s;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Client;
using KubeOps.Operator.Controller;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Serialization;
using KubeOps.Operator.Services;
using KubeOps.Operator.Watcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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

        internal IOperatorBuilder AddOperatorBase(OperatorSettings settings)
        {
            Services.AddSingleton(settings);

            // support lazy service resolution
            Services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

            Services.AddTransient(
                _ => new JsonSerializerSettings
                {
                    ContractResolver = new NamingConvention(),
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() },
                    },
                });
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new NamingConvention(),
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() },
                },
            };

            Services.AddTransient(
                _ => new SerializerBuilder()
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .WithNamingConvention(new NamingConvention())
                    .Build());

            Services.AddTransient<EntitySerializer>();

            Services.AddTransient<IKubernetesClient, KubernetesClient>();
            Services.AddSingleton<IKubernetes>(
                _ =>
                {
                    var config = KubernetesClientConfiguration.BuildDefaultConfig();

                    return new Kubernetes(config, new ClientUrlFixer())
                    {
                        SerializationSettings =
                        {
                            ContractResolver = new NamingConvention(),
                            Converters = new List<JsonConverter>
                                { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } },
                        },
                        DeserializationSettings =
                        {
                            ContractResolver = new NamingConvention(),
                            Converters = new List<JsonConverter>
                                { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } },
                        },
                    };
                });

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

            return this;
        }
    }
}
