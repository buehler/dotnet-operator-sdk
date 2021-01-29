using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Kubernetes;

namespace KubeOps.Operator.Controller
{
    /// <summary>
    /// Generic entity controller interface.
    /// This interface is primarily used for generic type help.
    /// </summary>
    /// <typeparam name="TEntity">The type of the kubernetes entity.</typeparam>
    public interface IResourceController<in TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Called for <see cref="ResourceEventType.Created"/> events for a given entity.
        /// </summary>
        /// <param name="entity">The entity that fired the created event.</param>
        /// <returns>
        /// A task with an optional <see cref="ResourceControllerResult"/>.
        /// Use the static constructors on the <see cref="ResourceControllerResult"/> class
        /// to create your controller function result.
        /// </returns>
        Task<ResourceControllerResult?> CreatedAsync(TEntity entity) =>
            Task.FromResult<ResourceControllerResult?>(null);

        /// <summary>
        /// Called for <see cref="ResourceEventType.Updated"/> events for a given entity.
        /// </summary>
        /// <param name="entity">The entity that fired the updated event.</param>
        /// <returns>
        /// A task with an optional <see cref="ResourceControllerResult"/>.
        /// Use the static constructors on the <see cref="ResourceControllerResult"/> class
        /// to create your controller function result.
        /// </returns>
        Task<ResourceControllerResult?> UpdatedAsync(TEntity entity) =>
            Task.FromResult<ResourceControllerResult?>(null);

        /// <summary>
        /// Called for <see cref="ResourceEventType.NotModified"/> events for a given entity.
        /// </summary>
        /// <param name="entity">The entity that fired the not-modified event.</param>
        /// <returns>
        /// A task with an optional <see cref="ResourceControllerResult"/>.
        /// Use the static constructors on the <see cref="ResourceControllerResult"/> class
        /// to create your controller function result.
        /// </returns>
        Task<ResourceControllerResult?> NotModifiedAsync(TEntity entity) =>
            Task.FromResult<ResourceControllerResult?>(null);

        /// <summary>
        /// Called for <see cref="ResourceEventType.StatusUpdated"/> events for a given entity.
        /// </summary>
        /// <param name="entity">The entity that fired the status-modified event.</param>
        /// <returns>
        /// A task that completes, when the reconciliation is done.
        /// </returns>
        Task StatusModifiedAsync(TEntity entity) =>
            Task.CompletedTask;

        /// <summary>
        /// Called for <see cref="ResourceEventType.Deleted"/> events for a given entity.
        /// </summary>
        /// <param name="entity">The entity that fired the deleted event.</param>
        /// <returns>
        /// A task that completes, when the reconciliation is done.
        /// </returns>
        Task DeletedAsync(TEntity entity) =>
            Task.CompletedTask;
    }
}
