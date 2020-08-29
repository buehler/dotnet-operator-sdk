using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator;
using KubeOps.Operator.Client;
using KubeOps.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Testing
{
    public class KubernetesTestOperator : KubernetesOperator
    {
        public KubernetesTestOperator(OperatorSettings settings)
            : base(settings)
        {
        }

        public IServiceProvider Services { get; private set; } = new ServiceCollection().BuildServiceProvider();

        public MockKubernetesClient MockedClient =>
            Services.GetRequiredService<IKubernetesClient>() as MockKubernetesClient ??
            throw new ArgumentException("Wrong kubernetes client registered.");

        public MockResourceEventQueue<TEntity> GetMockedEventQueue<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
            => Services.GetRequiredService<MockResourceQueueCollection>().Get<TEntity>();

        public override Task<int> Run(string[] args)
        {
            base.Run(args).ConfigureAwait(false);
            Services = OperatorHost?.Services ?? throw new ArgumentException("Host not built.");
            return Task.FromResult(0);
        }

        protected override void ConfigureOperatorServices()
        {
            ConfigureServices(
                services =>
                {
                    services.RemoveAll(typeof(IResourceEventQueue<>));
                    services.AddTransient(typeof(IResourceEventQueue<>), typeof(MockResourceEventQueue<>));
                    services.AddSingleton<MockResourceQueueCollection>();

                    services.RemoveAll(typeof(IKubernetesClient));
                    services.AddSingleton<IKubernetesClient, MockKubernetesClient>();
                });
            base.ConfigureOperatorServices();
        }

        protected override void ConfigureOperatorLogging(IEnumerable<string> _) =>
            Builder.ConfigureLogging(
                (__, logging) => { logging.ClearProviders(); });
    }
}
