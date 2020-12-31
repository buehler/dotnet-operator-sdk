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
    /// <summary>
    /// List of services for a type of an entity.
    /// Those services are needed in the various
    /// components of the operator like the controller or the finalizer.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IResourceServices<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Logger factory.
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// API Client for querying the cluster.
        /// </summary>
        IKubernetesClient Client { get; }

        /// <summary>
        /// The cache for the resources.
        /// </summary>
        IResourceCache<TEntity> ResourceCache { get; }

        /// <summary>
        /// Event queue.
        /// </summary>
        IResourceEventQueue<TEntity> EventQueue { get; }

        /// <summary>
        /// List of available resource finalizers.
        /// </summary>
        Lazy<IEnumerable<IResourceFinalizer>> Finalizers { get; }

        /// <summary>
        /// The general settings for the operator.
        /// </summary>
        OperatorSettings Settings { get; }

        /// <summary>
        /// Helper for leader election process.
        /// </summary>
        ILeaderElection LeaderElection { get; }
    }
}
