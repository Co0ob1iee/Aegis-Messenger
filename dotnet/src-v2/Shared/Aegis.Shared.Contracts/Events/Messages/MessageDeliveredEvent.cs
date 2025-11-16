using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Messages;

/// <summary>
/// Domain event raised when a message is delivered to the recipient
/// </summary>
/// <param name="MessageId">Unique identifier of the message</param>
/// <param name="RecipientId">User ID of the recipient</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record MessageDeliveredEvent(
    Guid MessageId,
    Guid RecipientId,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
