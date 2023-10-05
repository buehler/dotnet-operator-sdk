using System.Collections.Concurrent;

using k8s;
using k8s.Models;

using Timer = System.Timers.Timer;

namespace KubeOps.Operator.Queue;

internal class TimedEntityQueue<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly ConcurrentDictionary<string, (string Name, string? Namespace, Timer Timer)> _queue = new();

    public event EventHandler<(string Name, string? Namespace)>? RequeueRequested;

    internal int Count => _queue.Count;

    public void Clear()
    {
        foreach (var (_, _, timer) in _queue.Values)
        {
            timer.Stop();
        }

        _queue.Clear();
    }

    public void Enqueue(TEntity entity, TimeSpan requeueIn)
    {
        var (_, _, timer) =
            _queue.AddOrUpdate(
                entity.Uid(),
                (entity.Name(), entity.Namespace(), new Timer(requeueIn.TotalMilliseconds)),
                (_, e) =>
                {
                    e.Timer.Stop();
                    e.Timer.Dispose();
                    return (e.Name, e.Namespace, new Timer(requeueIn.TotalMilliseconds));
                });

        timer.Elapsed += (_, _) =>
        {
            if (!_queue.TryRemove(entity.Metadata.Uid, out var e))
            {
                return;
            }

            e.Timer.Stop();
            e.Timer.Dispose();
            RequeueRequested?.Invoke(this, (e.Name, e.Namespace));
        };
        timer.Start();
    }

    public void RemoveIfQueued(TEntity entity)
    {
        if (_queue.TryRemove(entity.Uid(), out var entry))
        {
            entry.Timer.Stop();
        }
    }
}
