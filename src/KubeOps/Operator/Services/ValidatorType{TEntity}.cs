using System;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Services
{
    internal record ValidatorType<TEntity> : ValidatorType
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        internal ValidatorType(Type instanceType)
            : base(instanceType, typeof(TEntity))
        {
        }
    }
}
