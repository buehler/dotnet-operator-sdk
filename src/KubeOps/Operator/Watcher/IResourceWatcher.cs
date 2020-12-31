using System;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Watcher
{
    /// <summary>
    /// Watcher for a given resource.
    /// Queries the kubernetes cluster and notifies the event-queue when something happens.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IResourceWatcher<TEntity> : IDisposable
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Event that fires when the kubernetes cluster notifies the watcher.
        /// </summary>
        event EventHandler<(WatchEventType Type, TEntity Resource)>? WatcherEvent;

        /// <summary>
        /// Start the watcher.
        /// </summary>
        /// <returns>A task that resolves when everything is done.</returns>
        Task Start();

        /// <summary>
        /// Stop the watcher.
        /// </summary>
        /// <returns>A task that resolves when everything is done.</returns>
        Task Stop();
    }
}
