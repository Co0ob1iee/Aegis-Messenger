using Aegis.Modules.Auth.Domain.Entities;

namespace Aegis.Modules.Auth.Application.Abstractions;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT access token for user
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generate refresh token
    /// </summary>
    /// <returns>Cryptographically random refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Get access token expiration time
    /// </summary>
    DateTime GetAccessTokenExpiration();

    /// <summary>
    /// Get refresh token expiration time
    /// </summary>
    DateTime GetRefreshTokenExpiration();

    /// <summary>
    /// Validate JWT token and extract user ID
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Guid? ValidateToken(string token);
}
