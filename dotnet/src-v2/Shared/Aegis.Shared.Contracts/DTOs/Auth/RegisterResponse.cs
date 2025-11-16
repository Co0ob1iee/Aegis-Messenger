namespace Aegis.Shared.Contracts.DTOs.Auth;

/// <summary>
/// Response after successful registration
/// </summary>
/// <param name="UserId">Unique identifier of the newly created user</param>
/// <param name="Username">Confirmed username</param>
/// <param name="Email">Confirmed email address</param>
public record RegisterResponse(
    Guid UserId,
    string Username,
    string Email
);
