using System;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;

namespace KubeOps.Operator.Queue
{
    /// <summary>
    /// Queue for resource events.
    /// This manages the queued events and handles re-queueing for requested
    /// delayed reconciliation.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IResourceEventQueue<TEntity> : IDisposable
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Event that fires when the resource modified its state.
        /// </summary>
        event EventHandler<(ResourceEventType Type, TEntity Resource)>? ResourceEvent;

        /// <summary>
        /// Start the queue.
        /// </summary>
        /// <returns>A task that resolves when everything is done.</returns>
        Task Start();

        /// <summary>
        /// Stop the queue from reading events.
        /// </summary>
        /// <returns>A task that resolves when everything is done.</returns>
        Task Stop();

        /// <summary>
        /// Enqueue the resource. The used event is determined by
        /// <see cref="IResourceCache{TEntity}"/> and the according
        /// <see cref="CacheComparisonResult"/>.
        /// </summary>
        /// <param name="resource">The resource to enqueue.</param>
        /// <param name="enqueueDelay">If given, a delay for the enqueue.</param>
        /// <returns>A task that resolves when everything is done.</returns>
        Task Enqueue(TEntity resource, TimeSpan? enqueueDelay = null);

        /// <summary>
        /// Enqueue an errored resource. This is used
        /// for "retry" mechanics. When a controller throws
        /// an exception, the resource is re-queued after an exponential
        /// backoff.
        /// </summary>
        /// <param name="type">The event type.</param>
        /// <param name="resource">The resource that errored.</param>
        void EnqueueErrored(ResourceEventType type, TEntity resource);

        /// <summary>
        /// Clear the error for a certain resource.
        /// </summary>
        /// <param name="resource">The resource that should be cleared.</param>
        void ClearError(TEntity resource);
    }
}
