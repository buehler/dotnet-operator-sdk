﻿using System;
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
    internal class MockManagedResourceController<TEntity> : ManagedResourceController<TEntity>
        where TEntity : class, IKubernetesObject<V1ObjectMeta>
    {
        public MockManagedResourceController(
            ILogger<ManagedResourceController<TEntity>> logger,
            IKubernetesClient client,
            ResourceWatcher<TEntity> watcher,
            ResourceCache<TEntity> cache,
            IServiceProvider services,
            ResourceControllerMetrics<TEntity> metrics,
            OperatorSettings settings,
            IFinalizerManager<TEntity> finalizerManager)
            : base(logger, client, watcher, cache, services, metrics, settings, finalizerManager)
        {
        }

        public override Task StartAsync() => Task.CompletedTask;

        public override Task StopAsync() => Task.CompletedTask;

        public Task EnqueueEvent(ResourceEventType type, TEntity resource) =>
            HandleResourceEvent(new QueuedEvent(type, resource));

        public Task EnqueueFinalization(TEntity resource) =>
            HandleResourceFinalization(new QueuedEvent(ResourceEventType.Finalizing, resource));
    }
}
