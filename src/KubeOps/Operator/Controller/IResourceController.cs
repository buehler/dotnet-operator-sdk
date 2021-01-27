using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Kubernetes;

namespace KubeOps.Operator.Controller
{
    /// <summary>
    /// Generic resource controller interface.
    /// This interface is primarily used for generic type help.
    /// </summary>
    /// <typeparam name="TResource">The type of the kubernetes resource.</typeparam>
    public interface IResourceController<in TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Called for <see cref="ResourceEventType.Created"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the created event.</param>
        /// <returns>
        /// A task with an optional <see cref="ResourceControllerResult"/>.
        /// Use the static constructors on the <see cref="ResourceControllerResult"/> class
        /// to create your controller function result.
        /// </returns>
        Task<ResourceControllerResult?> CreatedAsync(TResource resource) =>
            Task.FromResult<ResourceControllerResult?>(null);

        /// <summary>
        /// Called for <see cref="ResourceEventType.Updated"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the updated event.</param>
        /// <returns>
        /// A task with an optional <see cref="ResourceControllerResult"/>.
        /// Use the static constructors on the <see cref="ResourceControllerResult"/> class
        /// to create your controller function result.
        /// </returns>
        Task<ResourceControllerResult?> UpdatedAsync(TResource resource) =>
            Task.FromResult<ResourceControllerResult?>(null);

        /// <summary>
        /// Called for <see cref="ResourceEventType.NotModified"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the not-modified event.</param>
        /// <returns>
        /// A task with an optional <see cref="ResourceControllerResult"/>.
        /// Use the static constructors on the <see cref="ResourceControllerResult"/> class
        /// to create your controller function result.
        /// </returns>
        Task<ResourceControllerResult?> NotModifiedAsync(TResource resource) =>
            Task.FromResult<ResourceControllerResult?>(null);

        /// <summary>
        /// Called for <see cref="ResourceEventType.StatusUpdated"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the status-modified event.</param>
        /// <returns>
        /// A task that completes, when the reconciliation is done.
        /// </returns>
        Task StatusModifiedAsync(TResource resource) =>
            Task.CompletedTask;

        /// <summary>
        /// Called for <see cref="ResourceEventType.Deleted"/> events for a given resource.
        /// </summary>
        /// <param name="resource">The resource that fired the deleted event.</param>
        /// <returns>
        /// A task that completes, when the reconciliation is done.
        /// </returns>
        Task DeletedAsync(TResource resource) =>
            Task.CompletedTask;
    }
}
