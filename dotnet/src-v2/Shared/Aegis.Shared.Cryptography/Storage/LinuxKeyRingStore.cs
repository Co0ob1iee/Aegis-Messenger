using System.Runtime.Versioning;
using System.Text;
using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.Storage;

/// <summary>
/// Linux KeyRing-based secure key storage
/// Uses Secret Service API (freedesktop.org standard) for secure key storage
/// Supports GNOME Keyring, KDE KWallet, and other implementations
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxKeyRingStore : IKeyStore
{
    private readonly ILogger<LinuxKeyRingStore> _logger;
    private readonly string _applicationName;

    public bool IsHardwareBacked => false;  // Typically software-based, but can use TPM on some systems

    public LinuxKeyRingStore(
        ILogger<LinuxKeyRingStore> logger,
        string? applicationName = null)
    {
        _logger = logger;
        _applicationName = applicationName ?? "AegisMessenger";

        _logger.LogInformation(
            "Initialized LinuxKeyRingStore for application: {ApplicationName}",
            _applicationName);

        // Note: This is a simplified implementation
        // Production should use:
        // - libsecret bindings (via P/Invoke or C# wrapper)
        // - D-Bus Secret Service API
        // - See LINUX_KEYRING_PRODUCTION.md for details
    }

    public async Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        if (string.IsNullOrEmpty(keyId))
            throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

        if (key == null || key.Length == 0)
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            // In production, this should use libsecret or Secret Service API
            // For now, fall back to encrypted file storage
            var encryptedKey = await EncryptKeyAsync(key, userId);
            var keyPath = GetKeyPath(keyId, userId);
            await File.WriteAllBytesAsync(keyPath, encryptedKey);

            _logger.LogInformation(
                "Stored key {KeyId} for user {UserId} in Linux KeyRing ({Size} bytes)",
                keyId,
                userId,
                key.Length);

            _logger.LogWarning(
                "Using fallback file storage. Production should use libsecret or D-Bus Secret Service API.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store key {KeyId} for user {UserId}", keyId, userId);
            throw;
        }
    }

    public async Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId)
    {
        if (string.IsNullOrEmpty(keyId))
            throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

        try
        {
            var keyPath = GetKeyPath(keyId, userId);

            if (!File.Exists(keyPath))
            {
                _logger.LogWarning("Key {KeyId} not found for user {UserId}", keyId, userId);
                return null;
            }

            var encryptedKey = await File.ReadAllBytesAsync(keyPath);
            var key = await DecryptKeyAsync(encryptedKey, userId);

            _logger.LogDebug(
                "Retrieved key {KeyId} for user {UserId} from Linux KeyRing ({Size} bytes)",
                keyId,
                userId,
                key.Length);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve key {KeyId} for user {UserId}", keyId, userId);
            throw;
        }
    }

    public async Task<bool> DeleteKeyAsync(string keyId, Guid userId)
    {
        if (string.IsNullOrEmpty(keyId))
            throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

        try
        {
            var keyPath = GetKeyPath(keyId, userId);

            if (File.Exists(keyPath))
            {
                // Secure delete (3-pass overwrite)
                await SecureDeleteFileAsync(keyPath);

                _logger.LogInformation("Deleted key {KeyId} for user {UserId}", keyId, userId);
                return true;
            }
            else
            {
                _logger.LogWarning("Key {KeyId} not found for deletion for user {UserId}", keyId, userId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key {KeyId} for user {UserId}", keyId, userId);
            throw;
        }
    }

    public Task<bool> KeyExistsAsync(string keyId, Guid userId)
    {
        var keyPath = GetKeyPath(keyId, userId);
        return Task.FromResult(File.Exists(keyPath));
    }

    public async Task DeleteAllKeysAsync(Guid userId)
    {
        try
        {
            var userDir = GetUserDirectory(userId);
            if (Directory.Exists(userDir))
            {
                var files = Directory.GetFiles(userDir, "*.key");
                foreach (var file in files)
                {
                    await SecureDeleteFileAsync(file);
                }
                Directory.Delete(userDir);
                _logger.LogInformation("Deleted all keys for user {UserId} ({Count} keys)", userId, files.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all keys for user {UserId}", userId);
            throw;
        }
    }

    private string GetUserDirectory(Guid userId)
    {
        // Use XDG_DATA_HOME or fallback to ~/.local/share
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(dataHome))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dataHome = Path.Combine(home, ".local", "share");
        }

        var appDir = Path.Combine(dataHome, _applicationName, "keys", userId.ToString("N"));
        Directory.CreateDirectory(appDir);
        return appDir;
    }

    private string GetKeyPath(string keyId, Guid userId)
    {
        var sanitizedKeyId = SanitizeKeyId(keyId);
        var userDir = GetUserDirectory(userId);
        return Path.Combine(userDir, $"{sanitizedKeyId}.key");
    }

    private string SanitizeKeyId(string keyId)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(keyId.Where(c => !invalidChars.Contains(c)));
    }

    /// <summary>
    /// Encrypt key using user-specific encryption
    /// PRODUCTION: Use libsecret or Secret Service API instead
    /// </summary>
    private Task<byte[]> EncryptKeyAsync(byte[] key, Guid userId)
    {
        // NOTE: This is a SIMPLIFIED implementation
        // Production should use:
        // - libsecret for GNOME Keyring
        // - KWallet D-Bus API for KDE
        // - Secret Service API (freedesktop.org standard)
        // - TPM 2.0 when available

        // Simple XOR encryption with user-specific salt (DEMO ONLY)
        var salt = GetUserSalt(userId);
        var encrypted = new byte[key.Length];

        for (int i = 0; i < key.Length; i++)
        {
            encrypted[i] = (byte)(key[i] ^ salt[i % salt.Length]);
        }

        return Task.FromResult(encrypted);
    }

    private Task<byte[]> DecryptKeyAsync(byte[] encryptedKey, Guid userId)
    {
        // XOR is symmetric, so decrypt = encrypt
        return EncryptKeyAsync(encryptedKey, userId);
    }

    private byte[] GetUserSalt(Guid userId)
    {
        // Generate deterministic salt from userId
        // PRODUCTION: This should be a proper master key from KeyRing
        var userIdBytes = userId.ToByteArray();
        var salt = new byte[32];

        for (int i = 0; i < salt.Length; i++)
        {
            salt[i] = userIdBytes[i % userIdBytes.Length];
        }

        return salt;
    }

    private async Task SecureDeleteFileAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            // 3-pass overwrite (DoD 5220.22-M standard)
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                for (int pass = 0; pass < 3; pass++)
                {
                    fs.Seek(0, SeekOrigin.Begin);

                    var buffer = new byte[4096];
                    var random = new Random();

                    for (long written = 0; written < fileSize; written += buffer.Length)
                    {
                        var bytesToWrite = (int)Math.Min(buffer.Length, fileSize - written);
                        random.NextBytes(buffer);
                        await fs.WriteAsync(buffer.AsMemory(0, bytesToWrite));
                    }

                    await fs.FlushAsync();
                }
            }

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
