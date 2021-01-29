using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Kubernetes;
using KubeOps.Operator.Leadership;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Testing
{
    /// <summary>
    /// Operator factory for testing an operator created with the asp web server.
    /// </summary>
    /// <typeparam name="TTestStartup">Type of the Startup type (see asp.net).</typeparam>
    public class KubernetesOperatorFactory<TTestStartup> : WebApplicationFactory<TTestStartup>
        where TTestStartup : class
    {
        private string? _solutionRelativeContentRoot;

        /// <summary>
        /// Return a mocked kubernetes client. This client defines
        /// no-op for all related methods.
        /// </summary>
        public MockKubernetesClient MockedKubernetesClient =>
            Services.GetRequiredService<IKubernetesClient>() as MockKubernetesClient ??
            throw new ArgumentException("Wrong kubernetes client registered.");

        /// <summary>
        /// Set a specific content root path to the given factory.
        /// </summary>
        /// <param name="root">The solution relative content root path to configure.</param>
        /// <returns>The <see cref="KubernetesOperatorFactory{TTestStartup}"/> for chaining.</returns>
        public KubernetesOperatorFactory<TTestStartup> WithSolutionRelativeContentRoot(string root)
        {
            _solutionRelativeContentRoot = root;
            return this;
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Run()
        {
            // This triggers "EnsureServer()" in the base class.
            var server = Server;
        }

        public Task EnqueueEvent<TEntity>(ResourceEventType type, TEntity resource)
            where TEntity : class, IKubernetesObject<V1ObjectMeta>
        {
            var controller = Services.GetRequiredService<ManagedResourceController<TEntity>>() as
                MockManagedResourceController<TEntity>;

            return controller?.EnqueueEvent(type, resource) ?? Task.CompletedTask;
        }

        public Task EnqueueFinalization<TEntity>(TEntity resource)
            where TEntity : class, IKubernetesObject<V1ObjectMeta>
        {
            var controller = Services.GetRequiredService<ManagedResourceController<TEntity>>() as
                MockManagedResourceController<TEntity>;

            return controller?.EnqueueFinalization(resource) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Create the host builder. Needed for the factory.
        /// </summary>
        /// <returns>The created <see cref="IHostBuilder"/>.</returns>
        protected override IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(
                    webBuilder => webBuilder
                        .UseStartup<TTestStartup>());

        /// <summary>
        /// Configure the web-host.
        /// This registers the mocked client as well as mocked
        /// event queues.
        /// </summary>
        /// <param name="builder">The web host builder.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            if (_solutionRelativeContentRoot != null)
            {
                builder.UseSolutionRelativeContentRoot(_solutionRelativeContentRoot);
            }

            builder.ConfigureTestServices(
                services =>
                {
                    var elector = services.First(
                        d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(LeaderElector));
                    services.Remove(elector);

                    services.RemoveAll(typeof(IKubernetesClient));
                    services.AddSingleton<IKubernetesClient, MockKubernetesClient>();

                    services.RemoveAll(typeof(ManagedResourceController<>));
                    services.AddSingleton(typeof(ManagedResourceController<>), typeof(MockManagedResourceController<>));
                });
            builder.ConfigureLogging(logging => logging.ClearProviders());
        }
    }
}
