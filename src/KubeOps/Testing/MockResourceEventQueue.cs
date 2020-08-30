using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Queue;

namespace KubeOps.Testing
{
    public class MockResourceEventQueue<TEntity> : IResourceEventQueue<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly MockResourceQueueCollection _collection;

        public MockResourceEventQueue(MockResourceQueueCollection collection)
        {
            _collection = collection;
        }

        public event EventHandler<(ResourceEventType Type, TEntity Resource)>? ResourceEvent;

        public IList<TEntity> Enqueued { get; } = new List<TEntity>();

        public IList<(ResourceEventType Type, TEntity Resource)> ErrorEnqueued { get; } =
            new List<(ResourceEventType, TEntity)>();

        public void Dispose()
        {
        }

        public Task Start()
        {
            _collection.Register(this);
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _collection.Unregister(this);
            return Task.CompletedTask;
        }

        public void Created(TEntity entity) => Fire(ResourceEventType.Created, entity);

        public void Updated(TEntity entity) => Fire(ResourceEventType.Updated, entity);

        public void Deleted(TEntity entity) => Fire(ResourceEventType.Deleted, entity);

        public void NotModified(TEntity entity) => Fire(ResourceEventType.NotModified, entity);

        public void StatusUpdated(TEntity entity) => Fire(ResourceEventType.StatusUpdated, entity);

        public void Finalizing(TEntity entity) => Fire(ResourceEventType.Finalizing, entity);

        public Task Enqueue(TEntity resource, TimeSpan? enqueueDelay = null)
        {
            Enqueued.Add(resource);
            return Task.CompletedTask;
        }

        public void EnqueueErrored(ResourceEventType type, TEntity resource) => ErrorEnqueued.Add((type, resource));

        public void ClearError(TEntity resource)
        {
        }

        private void Fire(ResourceEventType type, TEntity entity) => ResourceEvent?.Invoke(this, (type, entity));
    }
}
