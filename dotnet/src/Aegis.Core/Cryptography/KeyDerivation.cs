using System;
using System.Security.Cryptography;
using System.Text;

namespace Aegis.Core.Cryptography;

/// <summary>
/// Key derivation functions (HKDF, PBKDF2)
/// Used for password hashing and key derivation
/// </summary>
public static class KeyDerivation
{
    private const int DefaultIterations = 310000; // OWASP recommendation 2023
    private const int SaltSize = 32;
    private const int KeySize = 32;

    /// <summary>
    /// Derive key from password using PBKDF2
    /// </summary>
    /// <param name="password">Password</param>
    /// <param name="salt">Salt (32 bytes)</param>
    /// <param name="iterations">Number of iterations</param>
    /// <returns>Derived key (32 bytes)</returns>
    public static byte[] DeriveKeyPBKDF2(string password, byte[] salt, int iterations = DefaultIterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(KeySize);
    }

    /// <summary>
    /// Derive key from password using HKDF (RFC 5869)
    /// </summary>
    /// <param name="inputKeyMaterial">Input key material</param>
    /// <param name="salt">Salt (optional)</param>
    /// <param name="info">Context-specific info (optional)</param>
    /// <param name="outputLength">Output key length</param>
    /// <returns>Derived key</returns>
    public static byte[] DeriveKeyHKDF(
        byte[] inputKeyMaterial,
        byte[]? salt = null,
        byte[]? info = null,
        int outputLength = KeySize)
    {
        salt ??= new byte[KeySize];
        info ??= Array.Empty<byte>();

        var output = new byte[outputLength];
        HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            inputKeyMaterial,
            output,
            salt,
            info);

        return output;
    }

    /// <summary>
    /// Generate cryptographically secure random salt
    /// </summary>
    public static byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    /// <summary>
    /// Hash password using Argon2 (if available, otherwise PBKDF2)
    /// </summary>
    public static (string hash, string salt) HashPassword(string password)
    {
        var salt = GenerateSalt();
        var hash = DeriveKeyPBKDF2(password, salt);

        return (
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt)
        );
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    public static bool VerifyPassword(string password, string hash, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var derivedHash = DeriveKeyPBKDF2(password, salt);
        var expectedHash = Convert.FromBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(derivedHash, expectedHash);
    }
}
