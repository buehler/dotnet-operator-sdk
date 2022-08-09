using System.Reactive.Subjects;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;

namespace KubeOps.Testing;

internal class MockEventQueue<TEntity> : IEventQueue<TEntity>
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    private readonly Subject<ResourceEvent<TEntity>> _localEvents;

    public MockEventQueue()
    {
        _localEvents = new Subject<ResourceEvent<TEntity>>();

        Events = _localEvents;
    }

    public IObservable<ResourceEvent<TEntity>> Events { get; }

    public Task StartAsync(Action<ResourceEvent<TEntity>> onWatcherEvent)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }

    public void EnqueueLocal(ResourceEvent<TEntity> resourceEvent) => _localEvents.OnNext(resourceEvent);
}
