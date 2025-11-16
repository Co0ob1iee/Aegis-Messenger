using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Auth;

/// <summary>
/// Domain event raised when a user successfully logs in
/// </summary>
/// <param name="UserId">Unique identifier of the user</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record UserLoggedInEvent(
    Guid UserId,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
