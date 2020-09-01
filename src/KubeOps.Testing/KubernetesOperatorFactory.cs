using System;
using k8s;
using k8s.Models;
using KubeOps.Operator.Client;
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
    public class KubernetesOperatorFactory<TTestStartup> : WebApplicationFactory<TTestStartup>
        where TTestStartup : class
    {
        private string? _solutionRelativeContentRoot;

        public MockKubernetesClient MockedKubernetesClient =>
            Services.GetRequiredService<IKubernetesClient>() as MockKubernetesClient ??
            throw new ArgumentException("Wrong kubernetes client registered.");

        public KubernetesOperatorFactory<TTestStartup> WithSolutionRelativeContentRoot(string root)
        {
            _solutionRelativeContentRoot = root;
            return this;
        }

        public void Run()
        {
            // This triggers "EnsureServer()" in the base class.
            var server = Server;
        }

        public MockResourceEventQueue<TEntity> GetMockedEventQueue<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
            => Services.GetRequiredService<MockResourceQueueCollection>().Get<TEntity>();

        protected override IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(
                    webBuilder => webBuilder
                        .UseStartup<TTestStartup>());

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
