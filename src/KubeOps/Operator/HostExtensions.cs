using System.Threading.Tasks;
using KubeOps.Operator.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace KubeOps.Operator
{
    /// <summary>
    /// Extensions for the <see cref="IHost"/>.
    /// </summary>
    public static class HostExtensions
    {
        /// <summary>
        /// Run the operator with default settings.
        /// Creates the application, creates the constructor-injection
        /// and runs the application with the given arguments.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/>.</param>
        /// <param name="args">Program arguments.</param>
        /// <returns>Async task with completion result.</returns>
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
