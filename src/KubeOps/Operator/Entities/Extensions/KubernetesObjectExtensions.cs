using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities.Extensions
{
    public static class KubernetesObjectExtensions
    {
        public static TResource WithOwnerReference<TResource>(
            this TResource resource,
            IKubernetesObject<V1ObjectMeta> owner)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            resource.Metadata.EnsureOwnerReferences().Add(owner.MakeOwnerReference());
            return resource;
        }

        public static IList<V1OwnerReference> EnsureOwnerReferences(this V1ObjectMeta meta)
        {
            if (meta.OwnerReferences == null)
            {
                meta.OwnerReferences = new List<V1OwnerReference>();
            }

            return meta.OwnerReferences;
        }

        public static V1OwnerReference MakeOwnerReference(this IKubernetesObject<V1ObjectMeta> kubernetesObject)
            => new V1OwnerReference(
                kubernetesObject.ApiVersion,
                kubernetesObject.Kind,
                kubernetesObject.Metadata.Name,
                kubernetesObject.Metadata.Uid);

        public static V1ObjectReference MakeObjectReference(this IKubernetesObject<V1ObjectMeta> kubernetesObject)
            => new V1ObjectReference()
            {
                ApiVersion = kubernetesObject.ApiVersion,
                Kind = kubernetesObject.Kind,
                Name = kubernetesObject.Metadata.Name,
                NamespaceProperty = kubernetesObject.Metadata.NamespaceProperty,
                ResourceVersion = kubernetesObject.Metadata.ResourceVersion,
                Uid = kubernetesObject.Metadata.Uid,
            };

        /* commented pending fleshing this out and improving after confirming event best practices
        /// <summary>
        /// Create an event for a kubernetesObject that can be applied to the cluster using the Create on <see cref="Client.IKubernetesClient" />
        /// </summary>
        /// <param name="kubernetesObject">The object to create an event for</param>
        /// <param name="reason">A short reason tag, like Created, Updated, Reconciled</param>
        /// <param name="message">A human readable message to go with the event</param>
        /// <param name="type">A string corresponding to the event type, Normal or Warning</param>
        /// <param name="component">The component generating the event, it should be the name of the operator</param>
        /// <returns></returns>
        public static V1Event Event(this IKubernetesObject<V1ObjectMeta> kubernetesObject, string reason, string message, string type = "Normal", string? component = null) =>
            new V1Event
            {
                Metadata = new V1ObjectMeta { Name = $"{kubernetesObject.Name()}.{DateTimeOffset.UtcNow.ToFileTime()}", NamespaceProperty = kubernetesObject.Namespace() },
                InvolvedObject = kubernetesObject.MakeObjectReference(),
                Reason = reason,
                Source = new V1EventSource { Component = component },
                Count = 1,
                Message = message,
                FirstTimestamp = DateTime.UtcNow,
                LastTimestamp = DateTime.UtcNow,
                Type = type
            };
        */
    }
}
