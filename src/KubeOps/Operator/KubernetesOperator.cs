using System;
using System.Threading.Tasks;
using KubeOps.Operator.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace KubeOps.Operator
{
    public class KubernetesOperator<TStartup>
        where TStartup : class
    {
        private Action<IWebHostBuilder>? _configureWebHost;

        protected IHostBuilder Builder { get; } = Host
            .CreateDefaultBuilder()
            .UseConsoleLifetime();

        protected IHost? OperatorHost { get; set; }

        public KubernetesOperator<TStartup> ConfigureWebHost(Action<IWebHostBuilder> configure)
        {
            _configureWebHost = configure;
            return this;
        }

        public Task<int> Run(string[] args)
        {
            var app = new CommandLineApplication<RunOperator>();

            OperatorHost = Builder
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder.UseStartup<TStartup>();
                        _configureWebHost?.Invoke(webBuilder);
                    })
                .Build();

            app
                .Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(OperatorHost.Services);

            JsonConvert.DefaultSettings = () => OperatorHost.Services.GetRequiredService<JsonSerializerSettings>();

            return app.ExecuteAsync(args);
        }
    }
}
