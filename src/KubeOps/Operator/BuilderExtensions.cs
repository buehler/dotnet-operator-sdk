using System;
using System.Collections.Generic;
using k8s;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Client;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Serialization;
using KubeOps.Operator.Watcher;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

namespace KubeOps.Operator
{
    public static class BuilderExtensions
    {
        public static IOperatorBuilder AddKubernetesOperator(this IServiceCollection services, Action<OperatorSettings> configure)
        {
            var settings = new OperatorSettings();
            configure(settings);
            return AddKubernetesOperator(services, settings);
        }

        public static IOperatorBuilder AddKubernetesOperator(this IServiceCollection services, OperatorSettings settings)
        {
            services.AddSingleton(settings);

            // support lazy service resolution
            services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

            services.AddTransient(
                _ => new JsonSerializerSettings
                {
                    ContractResolver = new NamingConvention(),
                    Converters = new List<JsonConverter>
                        {
                            new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() },
                        },
                });

            services.AddTransient(
                _ => new SerializerBuilder()
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .WithNamingConvention(new NamingConvention())
                    .Build());

            services.AddTransient<EntitySerializer>();

            services.AddTransient<IKubernetesClient, KubernetesClient>();
            services.AddSingleton<IKubernetes>(
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

            services.AddTransient(typeof(IResourceCache<>), typeof(ResourceCache<>));
            services.AddTransient(typeof(IResourceWatcher<>), typeof(ResourceWatcher<>));
            services.AddTransient(typeof(IResourceEventQueue<>), typeof(ResourceEventQueue<>));
            services.AddTransient(typeof(ResourceServices<>));

            return new OperatorBuilder(services);
        }
    }
}
