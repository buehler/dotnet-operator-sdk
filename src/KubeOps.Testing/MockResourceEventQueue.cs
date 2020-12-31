using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Queue;

namespace KubeOps.Testing
{
    /// <summary>
    /// Mocked resource event queue.
    /// Helps unit/integration test a controller by firing events
    /// when needed.
    /// </summary>
    ///
    /// <example>
    /// var queue = _factory.GetMockedEventQueue{V1TestEntity}();
    /// queue.Created(new V1TestEntity());
    /// </example>
    /// <typeparam name="TEntity"></typeparam>
    public class MockResourceEventQueue<TEntity> : IResourceEventQueue<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly MockResourceQueueCollection _collection;

        public MockResourceEventQueue(MockResourceQueueCollection collection)
        {
            _collection = collection;
        }

        /// <inheritdoc />
        public event EventHandler<(ResourceEventType Type, TEntity Resource)>? ResourceEvent;

        /// <summary>
        /// List of enqueued entities.
        /// </summary>
        public IList<TEntity> Enqueued { get; } = new List<TEntity>();

        /// <summary>
        /// List of enqueued errors.
        /// </summary>
        public IList<(ResourceEventType Type, TEntity Resource)> ErrorEnqueued { get; } =
            new List<(ResourceEventType, TEntity)>();

        public void Dispose()
        {
        }

        /// <inheritdoc />
        public Task Start()
        {
            _collection.Register(this);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Stop()
        {
            _collection.Unregister(this);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fire a (mocked) <see cref="ResourceEventType.Created"/> event.
        /// </summary>
        /// <param name="entity">The entity that fires the event.</param>
        public void Created(TEntity entity) => Fire(ResourceEventType.Created, entity);

        /// <summary>
        /// Fire a (mocked) <see cref="ResourceEventType.Updated"/> event.
        /// </summary>
        /// <param name="entity">The entity that fires the event.</param>
        public void Updated(TEntity entity) => Fire(ResourceEventType.Updated, entity);

        /// <summary>
        /// Fire a (mocked) <see cref="ResourceEventType.Deleted"/> event.
        /// </summary>
        /// <param name="entity">The entity that fires the event.</param>
        public void Deleted(TEntity entity) => Fire(ResourceEventType.Deleted, entity);

        /// <summary>
        /// Fire a (mocked) <see cref="ResourceEventType.NotModified"/> event.
        /// </summary>
        /// <param name="entity">The entity that fires the event.</param>
        public void NotModified(TEntity entity) => Fire(ResourceEventType.NotModified, entity);

        /// <summary>
        /// Fire a (mocked) <see cref="ResourceEventType.StatusUpdated"/> event.
        /// </summary>
        /// <param name="entity">The entity that fires the event.</param>
        public void StatusUpdated(TEntity entity) => Fire(ResourceEventType.StatusUpdated, entity);

        /// <summary>
        /// Fire a (mocked) <see cref="ResourceEventType.Finalizing"/> event.
        /// </summary>
        /// <param name="entity">The entity that fires the event.</param>
        public void Finalizing(TEntity entity) => Fire(ResourceEventType.Finalizing, entity);

        /// <inheritdoc />
        public Task Enqueue(TEntity resource, TimeSpan? enqueueDelay = null)
        {
            Enqueued.Add(resource);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void EnqueueErrored(ResourceEventType type, TEntity resource) => ErrorEnqueued.Add((type, resource));

        /// <inheritdoc />
        public void ClearError(TEntity resource)
        {
        }

        private void Fire(ResourceEventType type, TEntity entity) => ResourceEvent?.Invoke(this, (type, entity));
    }
}
