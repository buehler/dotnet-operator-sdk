using k8s;
using k8s.Models;
using KubeOps.Operator.Kubernetes;

namespace KubeOps.Operator.Controller;

public record ResourceEvent<TEntity>(ResourceEventType Type, TEntity Resource, int Attempt = 0, TimeSpan? Delay = null)
    where TEntity : class, IKubernetesObject<V1ObjectMeta>;
