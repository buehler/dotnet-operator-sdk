using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KubeOps.Operator.Commands;
using KubeOps.Operator.Logging;
using KubeOps.Testing;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KubeOps.Operator
{
    public class KubernetesOperator
    {
        internal const string NoStructuredLogs = "--no-structured-logs";
        private const string DefaultOperatorName = "KubernetesOperator";

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

        protected OperatorSettings OperatorSettings { get; }

        protected IList<Action<IServiceCollection>> ServiceConfigurations { get; } =
            new List<Action<IServiceCollection>>();

        protected IHostBuilder Builder { get; } = Host
            .CreateDefaultBuilder()
            .UseConsoleLifetime();

        protected IHost? OperatorHost { get; set; }

        public KubernetesTestOperator ToKubernetesTestOperator()
        {
            var op = new KubernetesTestOperator(OperatorSettings);

            foreach (var config in ServiceConfigurations)
            {
                op.ConfigureServices(config);
            }

            return op;
        }

        public Task<int> Run() => Run(new string[0]);

        public virtual Task<int> Run(string[] args)
            => Run(args, null);

        public KubernetesOperator ConfigureServices(Action<IServiceCollection> configuration)
        {
            ServiceConfigurations.Add(configuration);
            return this;
        }

        protected Task<int> Run(string[] args, Action? onHostBuilt)
        {
            ConfigureOperatorServices();

            var app = new CommandLineApplication<RunOperator>();

            ConfigureOperatorLogging(args);

            OperatorHost = Builder
                .ConfigureWebHostDefaults(
                    webBuilder => webBuilder
                        .UseStartup<OperatorStartup>()
                        .ConfigureKestrel(
                            kestrel =>
                            {
                                kestrel.AddServerHeader = false;
                                kestrel.ListenAnyIP(OperatorSettings.Port);
                            }))
                .Build();

            app
                .Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(OperatorHost.Services);

            JsonConvert.DefaultSettings = () => OperatorHost.Services.GetRequiredService<JsonSerializerSettings>();

            onHostBuilt?.Invoke();

            return app.ExecuteAsync(args);
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

        private void ConfigureRequiredServices() => Builder.ConfigureServices(
                services => services.AddKubernetesOperator(OperatorSettings));
    }
}
