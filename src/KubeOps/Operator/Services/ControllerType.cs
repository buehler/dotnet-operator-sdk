using System;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;

namespace KubeOps.Operator.Services
{
    // internal record ControllerType
    public record ControllerType
    {
        // internal ControllerType(Type instanceType, Type entityType)
        public ControllerType(Type instanceType, Type entityType)
        {
            if (!entityType.IsAssignableTo(typeof(IKubernetesObject<V1ObjectMeta>)))
            {
                throw new ArgumentException(
                    "Entity type must inherit from IKubernetesObject<V1ObjectMeta>",
                    nameof(entityType));
            }

            if (!instanceType.IsAssignableTo(typeof(IResourceController<>).MakeGenericType(entityType)))
            {
                throw new ArgumentException(
                    $"Instance type must inherit from IResourceController<{entityType.Name}>",
                    nameof(instanceType));
            }

            InstanceType = instanceType;
            EntityType = entityType;
        }

        public Type InstanceType { get; }

        public Type EntityType { get; }
    }
}
