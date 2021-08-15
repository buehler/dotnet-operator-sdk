using System;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Services
{
    internal record FinalizerType<TEntity> : FinalizerType
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        internal FinalizerType(Type instanceType)
            : base(instanceType, typeof(TEntity))
        {
        }
    }
}
