using KubeOps.Operator.Kubernetes;

namespace KubeOps.Operator.Controller.Results;

/// <summary>
/// Result class for determining additional actions on a controller
/// reconciliation. The additional actions are determined by the effective
/// type that is returned.
/// </summary>
public abstract class ResourceControllerResult
{
    internal ResourceControllerResult(TimeSpan delay)
    {
        RequeueIn = delay;
    }

    internal ResourceControllerResult(TimeSpan delay, ResourceEventType eventType)
    {
        RequeueIn = delay;
        EventType = eventType;
    }

    /// <summary>
    /// Time that should be waited for a requeue.
    /// </summary>
    public TimeSpan RequeueIn { get; }

    /// <summary>
    /// Type of the event to be queued.
    /// </summary>
    public ResourceEventType? EventType { get; }

    /// <summary>
    /// Create a <see cref="ResourceControllerResult"/> that requeues a resource
    /// with a given delay. When the event fires (after the delay) the resource
    /// cache is consulted and the new <see cref="ResourceEventType"/> is calculated.
    /// Based on this new calculation, the new event triggers the according function.
    /// </summary>
    /// <param name="delay">
    /// The delay. Please note, that a delay of <see cref="TimeSpan.Zero"/>
    /// will result in an immediate trigger of the function. This can lead to infinite circles.
    /// </param>
    /// <returns>The <see cref="ResourceControllerResult"/> with the configured delay.</returns>
    public static ResourceControllerResult RequeueEvent(TimeSpan delay)
        => new RequeueEventResult(delay);

    /// <summary>
    /// Create a <see cref="ResourceControllerResult"/> that requeues a resource
    /// with a given delay. When the event fires (after the delay) the resource
    /// cache is ignored in favor the specified <see cref="ResourceEventType"/>.
    /// Based on the specified type, the new event triggers the according function.
    /// </summary>
    /// <param name="delay">
    /// The delay. Please note, that a delay of <see cref="TimeSpan.Zero"/>
    /// will result in an immediate trigger of the function. This can lead to infinite circles.
    /// </param>
    /// <param name="eventType">
    /// The event type to queue.
    /// </param>
    /// <returns>The <see cref="ResourceControllerResult"/> with the configured delay and event type.</returns>
    public static ResourceControllerResult RequeueEvent(TimeSpan delay, ResourceEventType eventType)
        => new RequeueEventResult(delay, eventType);
}
