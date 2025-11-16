namespace Aegis.Shared.Cryptography.Interfaces;

/// <summary>
/// Interface for key derivation functions
/// </summary>
public interface IKeyDerivation
{
    /// <summary>
    /// Derive key from password using PBKDF2
    /// </summary>
    byte[] DeriveKeyPBKDF2(string password, byte[] salt, int iterations = 310000);

    /// <summary>
    /// Derive key using HKDF (RFC 5869)
    /// </summary>
    byte[] DeriveKeyHKDF(byte[] inputKeyMaterial, byte[]? salt = null, byte[]? info = null, int outputLength = 32);

    /// <summary>
    /// Generate cryptographically secure random salt
    /// </summary>
    byte[] GenerateSalt();

    /// <summary>
    /// Hash password
    /// </summary>
    (string hash, string salt) HashPassword(string password);

    /// <summary>
    /// Verify password against hash
    /// </summary>
    bool VerifyPassword(string password, string hash, string saltBase64);
}
