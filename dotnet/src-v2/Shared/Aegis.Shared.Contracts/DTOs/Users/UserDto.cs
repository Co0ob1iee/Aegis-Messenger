namespace Aegis.Shared.Contracts.DTOs.Users;

/// <summary>
/// Data transfer object for user information
/// </summary>
/// <param name="Id">Unique user identifier</param>
/// <param name="Username">Username</param>
/// <param name="Email">Email address</param>
/// <param name="PhoneNumber">Phone number (optional)</param>
/// <param name="IsOnline">Current online status</param>
/// <param name="LastSeenAt">Last seen timestamp</param>
/// <param name="CreatedAt">Account creation timestamp</param>
public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? PhoneNumber,
    bool IsOnline,
    DateTime? LastSeenAt,
    DateTime CreatedAt
);
