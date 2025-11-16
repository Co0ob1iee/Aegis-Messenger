namespace Aegis.Shared.Contracts.DTOs.Auth;

/// <summary>
/// Response after successful authentication
/// </summary>
/// <param name="UserId">Unique identifier of the authenticated user</param>
/// <param name="Token">JWT access token</param>
/// <param name="RefreshToken">JWT refresh token for obtaining new access tokens</param>
/// <param name="ExpiresAt">Token expiration timestamp</param>
public record LoginResponse(
    Guid UserId,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);
