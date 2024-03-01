using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Events;

/// <summary>
/// This injectable delegate publishes events on entities. Events are created in the same
/// namespace as the provided entity. However, if no namespace is provided (for example in
/// cluster wide entities), the "default" namespace is used.
///
/// The delegate creates a <see cref="Corev1Event"/> if none does exist or updates the
/// count and last seen timestamp if the same event already fired.
///
/// Events have a hex encoded name of a SHA512 hash. For the delegate to update
/// an event, the entity, reason, message, and type must be the same.
/// </summary>
/// <param name="entity">The entity that is involved with the event.</param>
/// <param name="reason">The reason string. This should be a machine readable reason string.</param>
/// <param name="message">A human readable string for the event.</param>
/// <param name="type">The <see cref="EventType"/> of the event (either normal or warning).</param>
/// <returns>A task that finishes when the event is created or updated.</returns>
/// <example>
/// Controller that fires a simple reconcile event on any entity it encounters.
/// Note that the publication of an event does not trigger another reconcile.
/// <code>
/// public class V1TestEntityController : IEntityController&lt;V1TestEntity&gt;
/// {
///     private readonly EventPublisher _eventPublisher;
///
///     public V1TestEntityController()
///     {
///         _eventPublisher = eventPublisher;
///     }
///
///     public async Task ReconcileAsync(V1TestEntity entity, CancellationToken token)
///     {
///         await _eventPublisher(entity, "Reconciled", "Entity was reconciled.");
///     }
/// }
/// </code>
/// </example>
public delegate Task EventPublisher(
    IKubernetesObject<V1ObjectMeta> entity,
    string reason,
    string message,
    EventType type = EventType.Normal);
