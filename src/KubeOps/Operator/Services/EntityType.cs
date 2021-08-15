using System;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;

namespace KubeOps.Operator.Services
{
    internal record EntityType
    {
        internal EntityType(Type instanceType)
        {
            if (!instanceType.IsAssignableTo(typeof(IKubernetesObject<V1ObjectMeta>)))
            {
                throw new ArgumentException(
                    "Instance type must inherit from IKubernetesObject<V1ObjectMeta>",
                    nameof(instanceType));
            }

            InstanceType = instanceType;
        }

        public Type InstanceType { get; }
    }
}
