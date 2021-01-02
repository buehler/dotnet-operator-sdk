using System;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Queue;
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
        /// <param name="root"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Return a mocked event queue for the given entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <returns>A mocked event queue for the given type.</returns>
        public MockResourceEventQueue<TEntity> GetMockedEventQueue<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
            => Services.GetRequiredService<MockResourceQueueCollection>().Get<TEntity>();

        /// <summary>
        /// Create the host builder. Needed for the factory.
        /// </summary>
        /// <returns></returns>
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
                    services.RemoveAll(typeof(IResourceEventQueue<>));
                    services.AddTransient(typeof(IResourceEventQueue<>), typeof(MockResourceEventQueue<>));
                    services.AddSingleton<MockResourceQueueCollection>();

                    services.RemoveAll(typeof(IKubernetesClient));
                    services.AddSingleton<IKubernetesClient, MockKubernetesClient>();
                });
            builder.ConfigureLogging(logging => logging.ClearProviders());
        }
    }
}
