using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer;

internal record FinalizerRegistration<TEntity>(string Identifier, Type FinalizerType)
    where TEntity : IKubernetesObject<V1ObjectMeta>;
