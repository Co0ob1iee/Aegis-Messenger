using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Messages;

/// <summary>
/// Domain event raised when a message is read by the recipient
/// </summary>
/// <param name="MessageId">Unique identifier of the message</param>
/// <param name="RecipientId">User ID of the recipient who read the message</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record MessageReadEvent(
    Guid MessageId,
    Guid RecipientId,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
