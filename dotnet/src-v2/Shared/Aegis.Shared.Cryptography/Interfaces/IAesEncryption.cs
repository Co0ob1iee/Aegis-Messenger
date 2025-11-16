namespace Aegis.Shared.Cryptography.Interfaces;

/// <summary>
/// Interface for AES encryption/decryption services
/// </summary>
public interface IAesEncryption
{
    /// <summary>
    /// Encrypt data using AES-256-GCM
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <param name="key">256-bit encryption key</param>
    /// <returns>Encrypted data (nonce + ciphertext + tag)</returns>
    Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key);

    /// <summary>
    /// Decrypt data using AES-256-GCM
    /// </summary>
    /// <param name="encrypted">Encrypted data (nonce + ciphertext + tag)</param>
    /// <param name="key">256-bit encryption key</param>
    /// <returns>Decrypted plaintext</returns>
    Task<byte[]> DecryptAsync(byte[] encrypted, byte[] key);

    /// <summary>
    /// Encrypt a string
    /// </summary>
    Task<string> EncryptStringAsync(string plaintext, byte[] key);

    /// <summary>
    /// Decrypt a string
    /// </summary>
    Task<string> DecryptStringAsync(string encrypted, byte[] key);

    /// <summary>
    /// Generate a random encryption key
    /// </summary>
    byte[] GenerateKey();

    /// <summary>
    /// Hash data using SHA-256
    /// </summary>
    byte[] Hash(byte[] data);

    /// <summary>
    /// Hash a string using SHA-256
    /// </summary>
    string HashString(string data);
}
