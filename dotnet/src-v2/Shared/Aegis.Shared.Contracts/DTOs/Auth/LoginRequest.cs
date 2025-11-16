namespace Aegis.Shared.Contracts.DTOs.Auth;

/// <summary>
/// Request to authenticate a user
/// </summary>
/// <param name="Username">Username or email</param>
/// <param name="Password">Plain text password (will be hashed)</param>
public record LoginRequest(
    string Username,
    string Password
);
