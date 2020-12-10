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
    public interface IResourceServices<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        ILoggerFactory LoggerFactory { get; }

        IKubernetesClient Client { get; }

        IResourceCache<TEntity> ResourceCache { get; }

        IResourceEventQueue<TEntity> EventQueue { get; }

        Lazy<IEnumerable<IResourceFinalizer>> Finalizers { get; }

        OperatorSettings Settings { get; }

        ILeaderElection LeaderElection { get; }
    }
}
