using System.Security.Cryptography;
using System.Text;
using Aegis.Shared.Cryptography.Interfaces;

namespace Aegis.Shared.Cryptography.Encryption;

/// <summary>
/// Key derivation service implementation
/// Provides PBKDF2, HKDF, and password hashing functions
/// </summary>
public class KeyDerivationService : IKeyDerivation
{
    private const int DefaultIterations = 310000; // OWASP recommendation 2023
    private const int SaltSize = 32;
    private const int KeySize = 32;

    /// <inheritdoc/>
    public byte[] DeriveKeyPBKDF2(string password, byte[] salt, int iterations = DefaultIterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(KeySize);
    }

    /// <inheritdoc/>
    public byte[] DeriveKeyHKDF(
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

    /// <inheritdoc/>
    public byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    /// <inheritdoc/>
    public (string hash, string salt) HashPassword(string password)
    {
        var salt = GenerateSalt();
        var hash = DeriveKeyPBKDF2(password, salt);

        return (
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt)
        );
    }

    /// <inheritdoc/>
    public bool VerifyPassword(string password, string hash, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var derivedHash = DeriveKeyPBKDF2(password, salt);
        var expectedHash = Convert.FromBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(derivedHash, expectedHash);
    }
}
