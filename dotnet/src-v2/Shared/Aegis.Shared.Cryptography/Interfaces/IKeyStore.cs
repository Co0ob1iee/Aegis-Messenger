namespace Aegis.Shared.Cryptography.Interfaces;

/// <summary>
/// Interface for secure key storage
/// Platform-specific implementations for Windows (DPAPI) and Android (KeyStore)
/// </summary>
public interface IKeyStore
{
    /// <summary>
    /// Store encrypted key securely
    /// </summary>
    /// <param name="keyId">Unique identifier for the key</param>
    /// <param name="key">Key data to store</param>
    /// <param name="userId">User ID for key isolation</param>
    Task StoreKeyAsync(string keyId, byte[] key, Guid userId);

    /// <summary>
    /// Retrieve encrypted key
    /// </summary>
    /// <param name="keyId">Unique identifier for the key</param>
    /// <param name="userId">User ID for key isolation</param>
    /// <returns>Decrypted key data or null if not found</returns>
    Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId);

    /// <summary>
    /// Delete stored key
    /// </summary>
    /// <param name="keyId">Unique identifier for the key</param>
    /// <param name="userId">User ID for key isolation</param>
    Task<bool> DeleteKeyAsync(string keyId, Guid userId);

    /// <summary>
    /// Check if key exists
    /// </summary>
    /// <param name="keyId">Unique identifier for the key</param>
    /// <param name="userId">User ID for key isolation</param>
    Task<bool> KeyExistsAsync(string keyId, Guid userId);

    /// <summary>
    /// Delete all keys for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    Task DeleteAllKeysAsync(Guid userId);
}
