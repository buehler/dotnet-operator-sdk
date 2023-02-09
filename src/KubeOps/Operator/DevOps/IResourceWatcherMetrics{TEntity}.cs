using k8s;
using k8s.Models;
using Prometheus;

namespace KubeOps.Operator.DevOps;

public interface IResourceWatcherMetrics<TEntity> : IResourceWatcherMetrics
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
}

public interface IResourceWatcherMetrics
{
    IGauge Running { get; }

    ICounter WatchedEvents { get; }

    ICounter WatcherExceptions { get; }

    ICounter WatcherClosed { get; }
}
