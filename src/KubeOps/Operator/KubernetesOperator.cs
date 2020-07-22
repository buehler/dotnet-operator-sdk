using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using k8s;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Client;
using KubeOps.Operator.Commands;
using KubeOps.Operator.DependencyInjection;
using KubeOps.Operator.Logging;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Serialization;
using KubeOps.Operator.Watcher;
using KubeOps.Testing;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

namespace KubeOps.Operator
{
    public class KubernetesOperator
    {
        internal const string NoStructuredLogs = "--no-structured-logs";
        private const string DefaultOperatorName = "KubernetesOperator";

        protected readonly OperatorSettings OperatorSettings;

        protected readonly IList<Action<IServiceCollection>> ServiceConfigurations =
            new List<Action<IServiceCollection>>();

        protected readonly IHostBuilder Builder = Host
            .CreateDefaultBuilder()
            .UseConsoleLifetime();

        protected IHost? OperatorHost { get; set; }

        public KubernetesOperator()
            : this((Assembly.GetEntryAssembly()?.GetName().Name ?? DefaultOperatorName).ToLowerInvariant())
        {
        }

        public KubernetesOperator(string operatorName)
            : this(
                new OperatorSettings
                {
                    Name = operatorName,
                })
        {
        }

        public KubernetesOperator(OperatorSettings settings)
        {
            OperatorSettings = settings;
        }

        public KubernetesTestOperator ToKubernetesTestOperator()
        {
            var op = new KubernetesTestOperator
            {
                OperatorSettings = { Name = OperatorSettings.Name }
            };

            foreach (var config in ServiceConfigurations)
            {
                op.ConfigureServices(config);
            }

            return op;
        }

        public Task<int> Run() => Run(new string[0]);

        public virtual Task<int> Run(string[] args)
        {
            ConfigureOperatorServices();

            var app = new CommandLineApplication<RunOperator>();

            ConfigureOperatorLogging(args);

            OperatorHost = Builder.Build();

            app
                .Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(OperatorHost.Services);

            DependencyInjector.Services = OperatorHost.Services;
            JsonConvert.DefaultSettings = () => OperatorHost.Services.GetRequiredService<JsonSerializerSettings>();

            return app.ExecuteAsync(args);
        }

        public KubernetesOperator ConfigureWebHost(Action<IWebHostBuilder> builder)
        {
            Builder.ConfigureWebHostDefaults(builder);
            return this;
        }

        public KubernetesOperator ConfigureServices(Action<IServiceCollection> configuration)
        {
            ServiceConfigurations.Add(configuration);
            return this;
        }

        protected virtual void ConfigureOperatorServices()
        {
            ConfigureRequiredServices();
            foreach (var config in ServiceConfigurations)
            {
                Builder.ConfigureServices(config);
            }
        }

        protected virtual void ConfigureOperatorLogging(IEnumerable<string> args) =>
            Builder.ConfigureLogging(
                (hostContext, logging) =>
                {
                    logging.ClearProviders();

                    if (hostContext.HostingEnvironment.IsProduction())
                    {
                        if (args.Contains(NoStructuredLogs))
                        {
                            logging.AddConsole(
                                options =>
                                {
                                    options.TimestampFormat = @"[dd.MM.yyyy - HH:mm:ss] ";
                                    options.DisableColors = true;
                                });
                        }
                        else
                        {
                            logging.AddStructuredConsole();
                        }
                    }
                    else
                    {
                        logging.AddConsole(options => options.TimestampFormat = @"[HH:mm:ss] ");
                    }
                });

        private void ConfigureRequiredServices() =>
            Builder.ConfigureServices(
                services =>
                {
                    services.AddSingleton(OperatorSettings);

                    services.AddTransient(
                        _ => new JsonSerializerSettings
                        {
                            ContractResolver = new NamingConvention(),
                            Converters = new List<JsonConverter>
                                { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } },
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
                                        { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } }
                                },
                                DeserializationSettings =
                                {
                                    ContractResolver = new NamingConvention(),
                                    Converters = new List<JsonConverter>
                                        { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } }
                                }
                            };
                        });

                    services.AddTransient(typeof(IResourceCache<>), typeof(ResourceCache<>));
                    services.AddTransient(typeof(IResourceWatcher<>), typeof(ResourceWatcher<>));
                    services.AddTransient(typeof(IResourceEventQueue<>), typeof(ResourceEventQueue<>));
                });
    }
}
