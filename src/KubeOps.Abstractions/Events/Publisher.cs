using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Events;

/// <summary>
/// TODO
/// </summary>
/// <param name="entity">The resource that is involved with the event.</param>
/// <param name="reason">The reason string. This should be a machine readable reason string.</param>
/// <param name="message">A human readable string for the event.</param>
/// <param name="type">The type of the event.</param>
/// <returns>A task that finishes when the event is created or updated.</returns>
public delegate Task EventPublisher(
    IKubernetesObject<V1ObjectMeta> entity,
    string reason,
    string message,
    EventType type = EventType.Normal);
