using k8s;
using k8s.Models;

namespace KubeOps.Operator.Kubernetes;

internal interface IResourceWatcher<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    IObservable<ResourceWatcher<TEntity>.WatchEvent> WatchEvents { get; }

    Task StartAsync();

    Task StopAsync();
}
