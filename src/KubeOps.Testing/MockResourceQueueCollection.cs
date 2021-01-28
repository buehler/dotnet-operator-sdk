using System;
using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeOps.Testing
{
    /// <summary>
    /// Static collection for <see cref="MockResourceEventQueue{TEntity}"/>.
    /// When a queue is created, the collection gets notified and keeps
    /// a list of them.
    /// </summary>
    // public class MockResourceQueueCollection
    // {
    //     private readonly IDictionary<Type, object> _queues =
    //         new Dictionary<Type, object>();
    //
    //     /// <summary>
    //     /// Register a mocked queue.
    //     /// </summary>
    //     /// <param name="queue">The queue to be registered.</param>
    //     /// <typeparam name="TEntity">The type of the entity.</typeparam>
    //     public void Register<TEntity>(MockResourceEventQueue<TEntity> queue)
    //         where TEntity : IKubernetesObject<V1ObjectMeta>
    //     {
    //         _queues.Add(typeof(TEntity), queue);
    //     }
    //
    //     /// <summary>
    //     /// Remove a registered queue for a given type.
    //     /// </summary>
    //     /// <param name="_">Not used.</param>
    //     /// <typeparam name="TEntity">The type of the entity.</typeparam>
    //     public void Unregister<TEntity>(MockResourceEventQueue<TEntity> _)
    //         where TEntity : IKubernetesObject<V1ObjectMeta>
    //     {
    //         _queues.Remove(typeof(TEntity));
    //     }
    //
    //     /// <summary>
    //     /// Get a mocked event queue for a given entity type.
    //     /// </summary>
    //     /// <typeparam name="TEntity">The type of the entity.</typeparam>
    //     /// <returns>The <see cref="MockResourceEventQueue{TEntity}"/> for the entity.</returns>
    //     public MockResourceEventQueue<TEntity> Get<TEntity>()
    //         where TEntity : IKubernetesObject<V1ObjectMeta>
    //         => (MockResourceEventQueue<TEntity>)_queues[typeof(TEntity)];
    // }
}
