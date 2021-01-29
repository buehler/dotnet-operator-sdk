using System;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Controller;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Kubernetes;
using Microsoft.Extensions.Logging;

namespace KubeOps.Testing
{
    internal class MockManagedResourceController<TResource> : ManagedResourceController<TResource>
        where TResource : class, IKubernetesObject<V1ObjectMeta>
    {
        public MockManagedResourceController(
            ILogger<ManagedResourceController<TResource>> logger,
            IKubernetesClient client,
            ResourceWatcher<TResource> watcher,
            ResourceCache<TResource> cache,
            IServiceProvider services,
            ResourceControllerMetrics<TResource> metrics,
            OperatorSettings settings,
            IFinalizerManager<TResource> finalizerManager)
            : base(logger, client, watcher, cache, services, metrics, settings, finalizerManager)
        {
        }

        public override Task StartAsync() => Task.CompletedTask;

        public override Task StopAsync() => Task.CompletedTask;

        public Task EnqueueEvent(ResourceEventType type, TResource resource) =>
            HandleResourceEvent(new QueuedEvent(type, resource));

        public Task EnqueueFinalization(TResource resource) =>
            HandleResourceFinalization(new QueuedEvent(ResourceEventType.Finalizing, resource));
    }
}
