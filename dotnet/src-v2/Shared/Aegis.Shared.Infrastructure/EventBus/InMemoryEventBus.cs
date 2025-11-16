using Aegis.Shared.Kernel.Interfaces;
using MediatR;

namespace Aegis.Shared.Infrastructure.EventBus;

/// <summary>
/// In-memory event bus implementation using MediatR
/// Publishes events to handlers within the same process
/// For distributed systems, use message queue (RabbitMQ, Azure Service Bus, etc.)
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly IPublisher _publisher;

    public InMemoryEventBus(IPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        await _publisher.Publish(@event, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishManyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            await _publisher.Publish(@event, cancellationToken);
        }
    }
}
