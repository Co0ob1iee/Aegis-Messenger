using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aegis.Core.Cryptography;

/// <summary>
/// Provides AES encryption/decryption services
/// Uses AES-256-GCM for authenticated encryption
/// </summary>
public class EncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private const int KeySize = 32; // 256 bits
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16; // 128 bits

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Encrypt data using AES-256-GCM
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <param name="key">256-bit encryption key</param>
    /// <returns>Encrypted data (nonce + ciphertext + tag)</returns>
    public async Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

        try
        {
            // Generate random nonce
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Prepare output buffer
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

            _logger.LogDebug("Encrypted {Size} bytes", plaintext.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw;
        }
    }

    /// <summary>
    /// Decrypt data using AES-256-GCM
    /// </summary>
    /// <param name="encrypted">Encrypted data (nonce + ciphertext + tag)</param>
    /// <param name="key">256-bit encryption key</param>
    /// <returns>Decrypted plaintext</returns>
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

            _logger.LogDebug("Decrypted {Size} bytes", plaintext.Length);
            return plaintext;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed - authentication tag mismatch");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw;
        }
    }

    /// <summary>
    /// Encrypt a string
    /// </summary>
    public async Task<string> EncryptStringAsync(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = await EncryptAsync(plaintextBytes, key);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Decrypt a string
    /// </summary>
    public async Task<string> DecryptStringAsync(string encrypted, byte[] key)
    {
        var encryptedBytes = Convert.FromBase64String(encrypted);
        var plaintext = await DecryptAsync(encryptedBytes, key);
        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Generate a random encryption key
    /// </summary>
    public byte[] GenerateKey()
    {
        var key = new byte[KeySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    /// Encrypt a file
    /// </summary>
    public async Task EncryptFileAsync(string inputPath, string outputPath, byte[] key)
    {
        try
        {
            var plaintext = await File.ReadAllBytesAsync(inputPath);
            var encrypted = await EncryptAsync(plaintext, key);
            await File.WriteAllBytesAsync(outputPath, encrypted);

            _logger.LogInformation("Encrypted file: {InputPath} -> {OutputPath}",
                inputPath, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File encryption failed: {InputPath}", inputPath);
            throw;
        }
    }

    /// <summary>
    /// Decrypt a file
    /// </summary>
    public async Task DecryptFileAsync(string inputPath, string outputPath, byte[] key)
    {
        try
        {
            var encrypted = await File.ReadAllBytesAsync(inputPath);
            var plaintext = await DecryptAsync(encrypted, key);
            await File.WriteAllBytesAsync(outputPath, plaintext);

            _logger.LogInformation("Decrypted file: {InputPath} -> {OutputPath}",
                inputPath, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File decryption failed: {InputPath}", inputPath);
            throw;
        }
    }

    /// <summary>
    /// Hash data using SHA-256
    /// </summary>
    public byte[] Hash(byte[] data)
    {
        return SHA256.HashData(data);
    }

    /// <summary>
    /// Hash a string using SHA-256
    /// </summary>
    public string HashString(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
