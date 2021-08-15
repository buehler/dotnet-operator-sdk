using System;
using k8s;
using k8s.Models;
using KubeOps.Operator.Finalizer;

namespace KubeOps.Operator.Services
{
    internal record FinalizerType
    {
        internal FinalizerType(Type instanceType, Type entityType)
        {
            if (!entityType.IsAssignableTo(typeof(IKubernetesObject<V1ObjectMeta>)))
            {
                throw new ArgumentException(
                    "Entity type must inherit from IKubernetesObject<V1ObjectMeta>",
                    nameof(entityType));
            }

            if (!instanceType.IsAssignableTo(typeof(IResourceFinalizer<>).MakeGenericType(entityType)))
            {
                throw new ArgumentException(
                    $"Instance type must inherit from IResourceFinalizer<{entityType.Name}>",
                    nameof(instanceType));
            }

            InstanceType = instanceType;
            EntityType = entityType;
        }

        public Type InstanceType { get; }

        public Type EntityType { get; }
    }
}
