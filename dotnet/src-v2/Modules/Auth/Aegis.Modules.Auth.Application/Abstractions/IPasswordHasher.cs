using Aegis.Modules.Auth.Domain.ValueObjects;

namespace Aegis.Modules.Auth.Application.Abstractions;

/// <summary>
/// Service for hashing and verifying passwords
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password with salt</returns>
    HashedPassword HashPassword(string password);

    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="hashedPassword">Hashed password to compare against</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string password, HashedPassword hashedPassword);
}
