using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using k8s;
using KubeOps.Operator.Client;
using KubeOps.Operator.Commands;
using KubeOps.Operator.DependencyInjection;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KubeOps.Operator
{
    public sealed class KubernetesOperator
    {
        private const string DefaultOperatorName = "KubernetesOperator";
        private readonly OperatorSettings _operatorSettings;

        private readonly IHostBuilder _builder = Host
            .CreateDefaultBuilder()
            .UseConsoleLifetime();

        public KubernetesOperator(string? operatorName = null)
        {
            operatorName ??= (operatorName
                              ?? Assembly.GetEntryAssembly()?.GetName().Name
                              ?? DefaultOperatorName).ToLowerInvariant();
            _operatorSettings = new OperatorSettings
            {
                Name = operatorName,
            };
        }

        public KubernetesOperator(OperatorSettings settings)
        {
            _operatorSettings = settings;
        }

        public Task<int> Run(string[] args)
        {
            ConfigureRequiredServices();

            var app = new CommandLineApplication<RunOperator>();
            var host = _builder.Build();

            app
                .Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(host.Services);

            DependencyInjector.Services = host.Services;
            JsonConvert.DefaultSettings = () => host.Services.GetRequiredService<JsonSerializerSettings>();

            return app.ExecuteAsync(args);
        }

        public KubernetesOperator ConfigureServices(Action<IServiceCollection> configuration)
        {
            _builder.ConfigureServices(configuration);
            return this;
        }

        private void ConfigureRequiredServices() =>
            _builder.ConfigureServices(services =>
            {
                services.AddSingleton(_operatorSettings);

                services.AddTransient(
                    _ => new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        Converters = new List<JsonConverter>
                            {new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()}},
                    });
                services.AddTransient(
                    _ => new SerializerBuilder()
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
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
                                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                Converters = new List<JsonConverter>
                                    {new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()}}
                            },
                            DeserializationSettings =
                            {
                                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                Converters = new List<JsonConverter>
                                    {new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()}}
                            }
                        };
                    });
            });
    }
}
