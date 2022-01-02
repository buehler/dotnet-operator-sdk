using k8s.Models;

namespace KubeOps.Operator.Events;

/// <summary>
/// The type of a <see cref="Corev1Event"/>.
/// The event type will be stringified and used as <see cref="Corev1Event.Type"/>.
/// </summary>
public enum EventType
{
    /// <summary>
    /// A normal event, informative value.
    /// </summary>
    Normal,

    /// <summary>
    /// A warning, something might went wrong.
    /// </summary>
    Warning,
}
