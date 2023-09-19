using k8s;
using k8s.Models;

using Prometheus;

namespace KubeOps.Operator.DevOps;

internal interface IResourceWatcherMetrics<TEntity> : IResourceWatcherMetrics
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
}

internal interface IResourceWatcherMetrics
{
    IGauge Running { get; }

    ICounter WatchedEvents { get; }

    ICounter WatcherExceptions { get; }

    ICounter WatcherClosed { get; }
}
