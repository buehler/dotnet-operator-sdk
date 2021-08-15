using System;
using k8s;
using k8s.Models;
using KubeOps.Operator.Webhooks;

namespace KubeOps.Operator.Services
{
    internal record MutatorType
    {
        internal MutatorType(Type instanceType, Type entityType)
        {
            if (!entityType.IsAssignableTo(typeof(IKubernetesObject<V1ObjectMeta>)))
            {
                throw new ArgumentException(
                    "Entity type must inherit from IKubernetesObject<V1ObjectMeta>",
                    nameof(entityType));
            }

            if (!instanceType.IsAssignableTo(typeof(IMutationWebhook<>).MakeGenericType(entityType)))
            {
                throw new ArgumentException(
                    $"Instance type must inherit from IMutationWebhook<{entityType.Name}>",
                    nameof(instanceType));
            }

            InstanceType = instanceType;
            EntityType = entityType;
        }

        public Type InstanceType { get; }

        public Type EntityType { get; }
    }
}
