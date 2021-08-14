using System;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Services
{
    internal record ControllerType<TEntity> : ControllerType
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        internal ControllerType(Type instanceType)
            : base(instanceType, typeof(TEntity))
        {
        }
    }
}
