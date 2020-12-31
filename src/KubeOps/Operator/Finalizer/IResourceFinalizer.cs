using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer
{
    /// <summary>
    /// Finalizer for a resource.
    /// </summary>
    public interface IResourceFinalizer
    {
        /// <summary>
        /// Unique identifier for this finalizer.
        /// </summary>
        /// <example>test.finalizer.dev.</example>
        string Identifier { get; }
    }

    /// <summary>
    /// Generic finalizer for a resource.
    /// </summary>
    /// <typeparam name="TResource">The type of the resources (entities).</typeparam>
    public interface IResourceFinalizer<in TResource> : IResourceFinalizer
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Register the finalizer for the given resource.
        /// </summary>
        /// <param name="resource">The resource to attach the finalizer to.</param>
        /// <returns>A task for completion.</returns>
        Task Register(TResource resource);

        internal Task FinalizeResource(TResource resource);
    }
}
