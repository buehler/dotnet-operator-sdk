using System.Collections.Generic;
using k8s;
using k8s.Models;
using KubeOps.Operator.Queue;

namespace KubeOps.Operator.Caching
{
    /// <summary>
    /// Resource cache for comparing objects and determine
    /// a <see cref="CacheComparisonResult"/>. This result
    /// is used to determine the event type for the <see cref="ResourceEventQueue{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of objects that are cached.</typeparam>
    public interface IResourceCache<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        /// <summary>
        /// Return an object from the cache.
        /// </summary>
        /// <param name="id">String id of the object.</param>
        /// <returns>The found entity.</returns>
        /// <exception cref="KeyNotFoundException">When the item does not exist.</exception>
        TEntity Get(string id);

        /// <summary>
        /// Insert or Update a given resource in the cache and determine the
        /// <see cref="CacheComparisonResult"/>.
        /// </summary>
        /// <param name="resource">The resource in question.</param>
        /// <param name="result"><see cref="CacheComparisonResult"/> for the given resource.</param>
        /// <returns>The inserted resource.</returns>
        TEntity Upsert(TEntity resource, out CacheComparisonResult result);

        /// <summary>
        /// Prefill the cache with a list of entities.
        /// This does not delete other items in the cache.
        /// </summary>
        /// <param name="entities">List of entities.</param>
        void Fill(IEnumerable<TEntity> entities);

        /// <summary>
        /// Remove an entity from the cache.
        /// If the resource is not present, this turns to a no-op.
        /// </summary>
        /// <param name="resource">The resource in question.</param>
        void Remove(TEntity resource);

        /// <summary>
        /// Clear the whole cache.
        /// </summary>
        public void Clear();
    }
}
