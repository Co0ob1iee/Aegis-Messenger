using System.Security.Cryptography;
using System.Text;
using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.Encryption;

/// <summary>
/// AES-256-GCM encryption service implementation
/// Provides authenticated encryption with additional data (AEAD)
/// </summary>
public class AesGcmEncryptionService : IAesEncryption
{
    private readonly ILogger<AesGcmEncryptionService> _logger;
    private const int KeySize = 32; // 256 bits
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16; // 128 bits

    public AesGcmEncryptionService(ILogger<AesGcmEncryptionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

        try
        {
            // Generate random nonce
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Prepare output buffers
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            // Encrypt using AES-GCM
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

            // Combine nonce + ciphertext + tag
            var result = new byte[NonceSize + ciphertext.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

            _logger.LogDebug("Encrypted {Size} bytes using AES-256-GCM", plaintext.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AES-GCM encryption failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> DecryptAsync(byte[] encrypted, byte[] key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

        if (encrypted.Length < NonceSize + TagSize)
            throw new ArgumentException("Encrypted data is too short", nameof(encrypted));

        try
        {
            // Extract nonce, ciphertext, and tag
            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ciphertextLength = encrypted.Length - NonceSize - TagSize;
            var ciphertext = new byte[ciphertextLength];

            Buffer.BlockCopy(encrypted, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encrypted, NonceSize, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(encrypted, NonceSize + ciphertextLength, tag, 0, TagSize);

            // Decrypt
            var plaintext = new byte[ciphertextLength];
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            _logger.LogDebug("Decrypted {Size} bytes using AES-256-GCM", plaintext.Length);
            return plaintext;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "AES-GCM decryption failed - authentication tag mismatch");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AES-GCM decryption failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> EncryptStringAsync(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = await EncryptAsync(plaintextBytes, key);
        return Convert.ToBase64String(encrypted);
    }

    /// <inheritdoc/>
    public async Task<string> DecryptStringAsync(string encrypted, byte[] key)
    {
        var encryptedBytes = Convert.FromBase64String(encrypted);
        var plaintext = await DecryptAsync(encryptedBytes, key);
        return Encoding.UTF8.GetString(plaintext);
    }

    /// <inheritdoc/>
    public byte[] GenerateKey()
    {
        var key = new byte[KeySize];
        RandomNumberGenerator.Fill(key);
        _logger.LogDebug("Generated new {KeySize}-bit AES key", KeySize * 8);
        return key;
    }

    /// <inheritdoc/>
    public byte[] Hash(byte[] data)
    {
        return SHA256.HashData(data);
    }

    /// <inheritdoc/>
    public string HashString(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
