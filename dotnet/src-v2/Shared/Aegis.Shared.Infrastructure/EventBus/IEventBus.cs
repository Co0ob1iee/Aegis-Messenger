using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Infrastructure.EventBus;

/// <summary>
/// Event bus for publishing and subscribing to domain events
/// Enables loose coupling between modules through event-driven architecture
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish a domain event to all registered handlers
    /// </summary>
    /// <typeparam name="TEvent">Type of domain event</typeparam>
    /// <param name="event">Event instance to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Publish multiple domain events
    /// </summary>
    /// <param name="events">Events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishManyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
