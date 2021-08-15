using System;
using k8s;
using k8s.Models;
using KubeOps.Operator.Webhooks;

namespace KubeOps.Operator.Services
{
    internal record ValidatorType
    {
        internal ValidatorType(Type instanceType, Type entityType)
        {
            if (!entityType.IsAssignableTo(typeof(IKubernetesObject<V1ObjectMeta>)))
            {
                throw new ArgumentException(
                    "Entity type must inherit from IKubernetesObject<V1ObjectMeta>",
                    nameof(entityType));
            }

            if (!instanceType.IsAssignableTo(typeof(IValidationWebhook<>).MakeGenericType(entityType)))
            {
                throw new ArgumentException(
                    $"Instance type must inherit from IValidationWebhook<{entityType.Name}>",
                    nameof(instanceType));
            }

            InstanceType = instanceType;
            EntityType = entityType;
        }

        public Type InstanceType { get; }

        public Type EntityType { get; }
    }
}
