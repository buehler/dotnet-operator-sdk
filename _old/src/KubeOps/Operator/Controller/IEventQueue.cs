using k8s;
using k8s.Models;

namespace KubeOps.Operator.Controller;

public interface IEventQueue<TEntity>
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    public IObservable<ResourceEvent<TEntity>> Events { get; }

    public Task StartAsync(Action<ResourceEvent<TEntity>> onWatcherEvent);

    public Task StopAsync();

    public void EnqueueLocal(ResourceEvent<TEntity> resourceEvent);
}
