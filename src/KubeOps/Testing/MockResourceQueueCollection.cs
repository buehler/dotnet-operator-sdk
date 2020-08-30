using System;
using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeOps.Testing
{
    public class MockResourceQueueCollection
    {
        private readonly IDictionary<Type, object> _queues =
            new Dictionary<Type, object>();

        public void Register<TEntity>(MockResourceEventQueue<TEntity> watcher)
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            _queues.Add(typeof(TEntity), watcher);
        }

        public void Unregister<TEntity>(MockResourceEventQueue<TEntity> _)
            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            _queues.Remove(typeof(TEntity));
        }

        public MockResourceEventQueue<TEntity> Get<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta>
            => (MockResourceEventQueue<TEntity>)_queues[typeof(TEntity)];
    }
}
