using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Events
{
    /// <summary>
    /// Event manager for <see cref="Corev1Event"/> objects.
    /// Contains various utility methods for emitting events on objects.
    /// </summary>
    public interface IEventManager
    {
        /// <summary>
        /// Delegate that publishes a predefined event for a statically given resource.
        /// This delegate should be created with
        /// <see cref="IEventManager.CreatePublisher(k8s.IKubernetesObject{k8s.Models.V1ObjectMeta},string,string,KubeOps.Operator.Events.EventType)"/>.
        /// When called, the publisher creates or updates the event defined by the params.
        /// </summary>
        /// <returns>A task that completes when the event is published.</returns>
        public delegate Task AsyncStaticPublisher();

        /// <summary>
        /// Delegate that publishes a predefined event for a resource that is passed.
        /// This delegate should be created with
        /// <see cref="IEventManager.CreatePublisher(string,string,KubeOps.Operator.Events.EventType)"/>.
        /// When called with a resource, the publisher creates or updates the event defined by the params.
        /// </summary>
        /// <param name="resource">The resource on which the event should be published.</param>
        /// <returns>A task that completes when the event is published.</returns>
        public delegate Task AsyncPublisher(IKubernetesObject<V1ObjectMeta> resource);

        /// <summary>
        /// PublishAsync an event in relation to a given resource.
        /// The event is created or updated if it exists.
        /// </summary>
        /// <param name="resource">The resource that is involved with the event.</param>
        /// <param name="reason">The reason string. This should be a machine readable reason string.</param>
        /// <param name="message">A human readable string for the event.</param>
        /// <param name="type">The type of the event.</param>
        /// <returns>A task that finishes when the event is created or updated.</returns>
        Task PublishAsync(
            IKubernetesObject<V1ObjectMeta> resource,
            string reason,
            string message,
            EventType type = EventType.Normal);

        /// <summary>
        /// Create or update an event.
        /// </summary>
        /// <param name="event">The full event object that should be created or updated.</param>
        /// <returns>A task that finishes when the event is created or updated.</returns>
        Task PublishAsync(Corev1Event @event);

        /// <summary>
        /// Create a <see cref="AsyncPublisher"/> for a predefined event.
        /// The <see cref="AsyncPublisher"/> is then called with a resource (<see cref="IKubernetesObject{V1ObjectMeta}"/>).
        /// The predefined event is published with this resource as the involved object.
        /// </summary>
        /// <param name="reason">The reason string. This should be a machine readable reason string.</param>
        /// <param name="message">A human readable string for the event.</param>
        /// <param name="type">The type of the event.</param>
        /// <returns>A <see cref="AsyncPublisher"/> delegate that can be called to create or update events.</returns>
        AsyncPublisher CreatePublisher(
            string reason,
            string message,
            EventType type = EventType.Normal);

        /// <summary>
        /// Create a <see cref="AsyncStaticPublisher"/> for a predefined event.
        /// The <see cref="AsyncStaticPublisher"/> is then called without any parameters.
        /// The predefined event is published with the initially given resource as the involved object.
        /// </summary>
        /// <param name="resource">The resource that is involved with the event.</param>
        /// <param name="reason">The reason string. This should be a machine readable reason string.</param>
        /// <param name="message">A human readable string for the event.</param>
        /// <param name="type">The type of the event.</param>
        /// <returns>A <see cref="AsyncStaticPublisher"/> delegate that can be called to create or update events.</returns>
        AsyncStaticPublisher CreatePublisher(
            IKubernetesObject<V1ObjectMeta> resource,
            string reason,
            string message,
            EventType type = EventType.Normal);
    }
}
