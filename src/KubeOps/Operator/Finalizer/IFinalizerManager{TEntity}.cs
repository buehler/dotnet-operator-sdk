using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer
{
    /// <summary>
    /// Finalizer manager to attach finalizer to resources.
    /// </summary>
    /// <typeparam name="TEntity">The type of the kubernetes entity.</typeparam>
    public interface IFinalizerManager<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Register a finalizer for the entity type on the provided instance.
        /// </summary>
        /// <param name="entity">The entity that will receive the finalizer.</param>
        /// <typeparam name="TFinalizer">The type of the finalizer.</typeparam>
        /// <returns>A task when the operation is done.</returns>
        Task RegisterFinalizerAsync<TFinalizer>(TEntity entity)
            where TFinalizer : IResourceFinalizer<TEntity>;

        /// <summary>
        /// Register all known finalizers for the entity type on the provided instance.
        /// </summary>
        /// <param name="entity">The entity that will receive the finalizers.</param>
        /// <returns>A task when the operation is done.</returns>
        Task RegisterAllFinalizersAsync(TEntity entity);

        internal Task FinalizeAsync(TEntity entity);
    }
}
