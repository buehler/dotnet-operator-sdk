using System;
using System.Collections.Generic;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Client;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Watcher;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Controller
{
    internal class ResourceServices<TEntity> : IResourceServices<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        public ResourceServices(
            ILoggerFactory loggerFactory,
            IKubernetesClient client,
            IResourceCache<TEntity> resourceCache,
            IResourceEventQueue<TEntity> eventQueue,
            Lazy<IEnumerable<IResourceFinalizer>> finalizers,
            OperatorSettings settings)
        {
            LoggerFactory = loggerFactory;
            Client = client;
            ResourceCache = resourceCache;
            EventQueue = eventQueue;
            Finalizers = finalizers;
            Settings = settings;
        }

        public ILoggerFactory LoggerFactory { get; }

        public IKubernetesClient Client { get; }

        public IResourceCache<TEntity> ResourceCache { get; }

        public IResourceEventQueue<TEntity> EventQueue { get; }

        public Lazy<IEnumerable<IResourceFinalizer>> Finalizers { get; }

        public OperatorSettings Settings { get; }
    }
}
