namespace KubeOps.Abstractions.Events;

/// <summary>
/// Represents a type used to create <see cref="EventPublisher"/>s for clients and controllers.
/// </summary>
public interface IEventPublisherFactory
{
    /// <summary>
    /// Creates a new event publisher.
    /// </summary>
    /// <returns>The <see cref="EventPublisher"/>.</returns>
    EventPublisher Create();
}
