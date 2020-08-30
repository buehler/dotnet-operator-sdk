using System;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Queue
{
    public interface IResourceEventQueue<TEntity> : IDisposable
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        event EventHandler<(ResourceEventType Type, TEntity Resource)>? ResourceEvent;

        Task Start();

        Task Stop();

        Task Enqueue(TEntity resource, TimeSpan? enqueueDelay = null);

        void EnqueueErrored(ResourceEventType type, TEntity resource);

        void ClearError(TEntity resource);
    }
}
