using System;
using System.Collections.Generic;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Leadership;
using KubeOps.Operator.Queue;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Services
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
            OperatorSettings settings,
            ILeaderElection leaderElection)
        {
            LoggerFactory = loggerFactory;
            Client = client;
            ResourceCache = resourceCache;
            EventQueue = eventQueue;
            Finalizers = finalizers;
            Settings = settings;
            LeaderElection = leaderElection;
        }

        public ILoggerFactory LoggerFactory { get; }

        public IKubernetesClient Client { get; }

        public IResourceCache<TEntity> ResourceCache { get; }

        public IResourceEventQueue<TEntity> EventQueue { get; }

        public Lazy<IEnumerable<IResourceFinalizer>> Finalizers { get; }

        public OperatorSettings Settings { get; }

        public ILeaderElection LeaderElection { get; }
    }
}
