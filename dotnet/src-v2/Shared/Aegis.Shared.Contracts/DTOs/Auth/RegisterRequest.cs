namespace Aegis.Shared.Contracts.DTOs.Auth;

/// <summary>
/// Request to register a new user
/// </summary>
/// <param name="Username">Unique username (3-50 characters, alphanumeric with underscore/dash)</param>
/// <param name="Email">Valid email address</param>
/// <param name="Password">Password (will be validated for strength requirements)</param>
/// <param name="PhoneNumber">Optional phone number in E.164 format</param>
public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? PhoneNumber = null
);
