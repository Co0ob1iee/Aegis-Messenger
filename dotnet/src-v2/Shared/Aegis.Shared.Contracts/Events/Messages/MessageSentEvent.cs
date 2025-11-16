using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Messages;

/// <summary>
/// Domain event raised when a message is sent
/// </summary>
/// <param name="MessageId">Unique identifier of the message</param>
/// <param name="SenderId">User ID of the sender</param>
/// <param name="RecipientId">User ID of the recipient</param>
/// <param name="IsGroup">Whether this is a group message</param>
/// <param name="GroupId">Group ID if this is a group message</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record MessageSentEvent(
    Guid MessageId,
    Guid SenderId,
    Guid RecipientId,
    bool IsGroup,
    Guid? GroupId,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
