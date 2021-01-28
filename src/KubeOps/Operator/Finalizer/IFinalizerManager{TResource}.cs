using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer
{
    /// <summary>
    /// Finalizer manager to attach finalizer to resources.
    /// </summary>
    /// <typeparam name="TResource">The type of the kubernetes resource.</typeparam>
    public interface IFinalizerManager<TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Register a finalizer for the resource on the resource.
        /// </summary>
        /// <param name="resource">The resource that will receive the finalizer.</param>
        /// <typeparam name="TFinalizer">The type of the finalizer.</typeparam>
        /// <returns>A task when the operation is done.</returns>
        Task RegisterFinalizerAsync<TFinalizer>(TResource resource)
            where TFinalizer : IResourceFinalizer<TResource>;

        internal Task FinalizeAsync(TResource resource);
    }
}
