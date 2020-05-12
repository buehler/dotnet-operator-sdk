using System;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Watcher
{
    internal interface IResourceWatcher<TEntity> : IDisposable
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        event EventHandler<(WatchEventType type, TEntity resource)>? WatcherEvent;

        Task Start();

        Task Stop();
    }
}
