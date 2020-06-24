using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator;
using KubeOps.Operator.DependencyInjection;
using KubeOps.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Testing
{
    public class KubernetesTestOperator : KubernetesOperator, IDisposable
    {
        public IServiceProvider Services => DependencyInjector.Services;

        public MockResourceEventQueue<TEntity> GetMockedEventQueue<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
            => Services.GetRequiredService<MockResourceQueueCollection>().Get<TEntity>();

        public void Dispose()
        {
            Services.GetService<IHost>()?.StopAsync();
        }

        public override Task<int> Run(string[] args)
        {
            base.Run(args).ConfigureAwait(false);
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
                });
            base.ConfigureOperatorServices();
        }

        protected override void ConfigureOperatorLogging(IEnumerable<string> _) =>
            Builder.ConfigureLogging(
                (__, logging) => { logging.ClearProviders(); });
    }
}
