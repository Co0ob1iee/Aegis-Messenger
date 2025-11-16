using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.Storage;

/// <summary>
/// Windows DPAPI-based secure key storage
/// Uses Windows Data Protection API to encrypt keys at rest
/// Keys are protected with user credentials (can only be decrypted by the same user)
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsDpapiKeyStore : IKeyStore
{
    private readonly ILogger<WindowsDpapiKeyStore> _logger;
    private readonly string _storagePath;
    private readonly byte[] _entropy;

    public bool IsHardwareBacked => false;  // DPAPI is software-based

    public WindowsDpapiKeyStore(
        ILogger<WindowsDpapiKeyStore> logger,
        string? storagePath = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "WindowsDpapiKeyStore is only supported on Windows");
        }

        _logger = logger;

        // Default storage path: %LOCALAPPDATA%\AegisMessenger\Keys
        _storagePath = storagePath ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AegisMessenger",
                "Keys");

        // Ensure directory exists
        Directory.CreateDirectory(_storagePath);

        // Generate entropy for additional security
        // In production, this should be stored separately or derived from machine-specific data
        _entropy = GenerateEntropy();

        _logger.LogInformation(
            "Initialized WindowsDpapiKeyStore with storage path: {Path}",
            _storagePath);
    }

    public async Task StoreKeyAsync(string keyId, byte[] key)
    {
        if (string.IsNullOrEmpty(keyId))
            throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

        if (key == null || key.Length == 0)
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            // Encrypt key using DPAPI
            var encryptedKey = ProtectedData.Protect(
                key,
                _entropy,
                DataProtectionScope.CurrentUser);

            // Store encrypted key to file
            var keyPath = GetKeyPath(keyId);
            await File.WriteAllBytesAsync(keyPath, encryptedKey);

            _logger.LogInformation(
                "Stored key {KeyId} ({Size} bytes encrypted to {EncryptedSize} bytes)",
                keyId,
                key.Length,
                encryptedKey.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store key {KeyId}", keyId);
            throw;
        }
    }

    public async Task<byte[]> RetrieveKeyAsync(string keyId)
    {
        if (string.IsNullOrEmpty(keyId))
            throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

        try
        {
            var keyPath = GetKeyPath(keyId);

            if (!File.Exists(keyPath))
            {
                throw new FileNotFoundException($"Key not found: {keyId}");
            }

            // Read encrypted key from file
            var encryptedKey = await File.ReadAllBytesAsync(keyPath);

            // Decrypt key using DPAPI
            var key = ProtectedData.Unprotect(
                encryptedKey,
                _entropy,
                DataProtectionScope.CurrentUser);

            _logger.LogDebug(
                "Retrieved key {KeyId} ({EncryptedSize} bytes decrypted to {Size} bytes)",
                keyId,
                encryptedKey.Length,
                key.Length);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve key {KeyId}", keyId);
            throw;
        }
    }

    public async Task DeleteKeyAsync(string keyId)
    {
        if (string.IsNullOrEmpty(keyId))
            throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

        try
        {
            var keyPath = GetKeyPath(keyId);

            if (File.Exists(keyPath))
            {
                // Overwrite file with random data before deletion (secure delete)
                await SecureDeleteFileAsync(keyPath);

                _logger.LogInformation("Deleted key {KeyId}", keyId);
            }
            else
            {
                _logger.LogWarning("Key {KeyId} not found for deletion", keyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key {KeyId}", keyId);
            throw;
        }
    }

    public async Task<bool> KeyExistsAsync(string keyId)
    {
        var keyPath = GetKeyPath(keyId);
        return File.Exists(keyPath);
    }

    public async Task<IEnumerable<string>> ListKeysAsync()
    {
        try
        {
            if (!Directory.Exists(_storagePath))
                return Enumerable.Empty<string>();

            var files = Directory.GetFiles(_storagePath, "*.key");
            return files.Select(f => Path.GetFileNameWithoutExtension(f));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list keys");
            throw;
        }
    }

    private string GetKeyPath(string keyId)
    {
        // Sanitize keyId to prevent directory traversal
        var sanitizedKeyId = SanitizeKeyId(keyId);
        return Path.Combine(_storagePath, $"{sanitizedKeyId}.key");
    }

    private string SanitizeKeyId(string keyId)
    {
        // Remove any path separators and invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(keyId.Where(c => !invalidChars.Contains(c)));
    }

    private byte[] GenerateEntropy()
    {
        // Generate machine-specific entropy
        // This adds an extra layer of protection beyond user credentials
        var entropy = new byte[32];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(entropy);

        // In production, you might want to:
        // 1. Derive entropy from machine GUID
        // 2. Store entropy separately (e.g., in registry)
        // 3. Use hardware identifiers

        return entropy;
    }

    private async Task SecureDeleteFileAsync(string filePath)
    {
        try
        {
            // Get file size
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            // Overwrite with random data (3 passes - DoD 5220.22-M standard)
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                for (int pass = 0; pass < 3; pass++)
                {
                    fs.Seek(0, SeekOrigin.Begin);

                    var buffer = new byte[4096];
                    using var rng = RandomNumberGenerator.Create();

                    for (long written = 0; written < fileSize; written += buffer.Length)
                    {
                        var bytesToWrite = (int)Math.Min(buffer.Length, fileSize - written);
                        rng.GetBytes(buffer, 0, bytesToWrite);
                        await fs.WriteAsync(buffer, 0, bytesToWrite);
                    }

                    await fs.FlushAsync();
                }
            }

            // Finally, delete the file
            File.Delete(filePath);

            _logger.LogDebug(
                "Securely deleted file {FilePath} ({Size} bytes, 3 overwrites)",
                filePath,
                fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to securely delete file {FilePath}", filePath);
            throw;
        }
    }
}
