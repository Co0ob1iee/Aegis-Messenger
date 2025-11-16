using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Groups;

/// <summary>
/// Domain event raised when a new group is created
/// </summary>
/// <param name="GroupId">Unique identifier of the group</param>
/// <param name="GroupName">Name of the group</param>
/// <param name="CreatorId">User ID of the group creator</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record GroupCreatedEvent(
    Guid GroupId,
    string GroupName,
    Guid CreatorId,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
