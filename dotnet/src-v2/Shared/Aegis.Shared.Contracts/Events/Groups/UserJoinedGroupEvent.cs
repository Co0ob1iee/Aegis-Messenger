using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Groups;

/// <summary>
/// Domain event raised when a user joins a group
/// </summary>
/// <param name="GroupId">Unique identifier of the group</param>
/// <param name="UserId">User ID of the user who joined</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record UserJoinedGroupEvent(
    Guid GroupId,
    Guid UserId,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
