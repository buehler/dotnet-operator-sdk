using System.Threading.Tasks;
using KubeOps.Operator.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace KubeOps.Operator
{
    public static class HostExtensions
    {
        public static Task<int> RunOperator(this IHost host, string[] args)
        {
            var app = new CommandLineApplication<RunOperator>();
            app
                .Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(host.Services);

            JsonConvert.DefaultSettings = () => host.Services.GetRequiredService<JsonSerializerSettings>();

            return app.ExecuteAsync(args);
        }
    }
}
