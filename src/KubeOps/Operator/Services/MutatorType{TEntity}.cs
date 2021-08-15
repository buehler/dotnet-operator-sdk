using System;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Services
{
    internal record MutatorType<TEntity> : MutatorType
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        internal MutatorType(Type instanceType)
            : base(instanceType, typeof(TEntity))
        {
        }
    }
}
