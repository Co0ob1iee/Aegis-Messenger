using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Contracts.Events.Auth;

/// <summary>
/// Domain event raised when a new user is registered
/// Other modules can listen to this event to perform related actions
/// </summary>
/// <param name="UserId">Unique identifier of the registered user</param>
/// <param name="Username">Username of the registered user</param>
/// <param name="Email">Email address of the registered user</param>
/// <param name="OccurredAt">Timestamp when the event occurred</param>
public record UserRegisteredEvent(
    Guid UserId,
    string Username,
    string Email,
    DateTime OccurredAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
