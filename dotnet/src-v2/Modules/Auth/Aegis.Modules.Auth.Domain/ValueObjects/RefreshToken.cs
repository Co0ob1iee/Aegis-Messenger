using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Modules.Auth.Domain.ValueObjects;

/// <summary>
/// Value object representing a JWT refresh token
/// Used for obtaining new access tokens without re-authentication
/// </summary>
public sealed class RefreshToken : ValueObject
{
    public string Token { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken(string token, DateTime expiresAt)
    {
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    /// <summary>
    /// Create a new refresh token
    /// </summary>
    /// <param name="token">Token string (should be cryptographically random)</param>
    /// <param name="expiresAt">Expiration timestamp</param>
    public static Result<RefreshToken> Create(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure<RefreshToken>(new Error(
                "RefreshToken.TokenEmpty",
                "Refresh token cannot be empty"));
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            return Result.Failure<RefreshToken>(new Error(
                "RefreshToken.InvalidExpiration",
                "Refresh token expiration must be in the future"));
        }

        return Result.Success(new RefreshToken(token, expiresAt));
    }

    /// <summary>
    /// Check if token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Check if token is valid (not expired and not revoked)
    /// </summary>
    public bool IsValid => !IsExpired && !IsRevoked;

    /// <summary>
    /// Revoke the token
    /// </summary>
    public void Revoke()
    {
        if (!IsRevoked)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Token;
        yield return ExpiresAt;
    }
}
