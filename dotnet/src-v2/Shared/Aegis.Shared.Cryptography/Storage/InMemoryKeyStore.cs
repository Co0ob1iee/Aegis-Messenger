using System.Collections.Concurrent;
using Aegis.Shared.Cryptography.Interfaces;

namespace Aegis.Shared.Cryptography.Storage;

/// <summary>
/// In-memory key storage implementation
/// WARNING: For development/testing only. Keys are lost when application restarts.
/// Production implementations should use:
/// - Windows: WindowsKeyStore with DPAPI
/// - Android: AndroidKeyStore with hardware-backed KeyStore
/// </summary>
public class InMemoryKeyStore : IKeyStore
{
    private readonly ConcurrentDictionary<string, byte[]> _keys = new();

    /// <inheritdoc/>
    public Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        var fullKeyId = GetFullKeyId(keyId, userId);
        _keys[fullKeyId] = (byte[])key.Clone();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId)
    {
        var fullKeyId = GetFullKeyId(keyId, userId);
        if (_keys.TryGetValue(fullKeyId, out var key))
        {
            return Task.FromResult<byte[]?>((byte[])key.Clone());
        }
        return Task.FromResult<byte[]?>(null);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteKeyAsync(string keyId, Guid userId)
    {
        var fullKeyId = GetFullKeyId(keyId, userId);
        return Task.FromResult(_keys.TryRemove(fullKeyId, out _));
    }

    /// <inheritdoc/>
    public Task<bool> KeyExistsAsync(string keyId, Guid userId)
    {
        var fullKeyId = GetFullKeyId(keyId, userId);
        return Task.FromResult(_keys.ContainsKey(fullKeyId));
    }

    /// <inheritdoc/>
    public Task DeleteAllKeysAsync(Guid userId)
    {
        var keysToRemove = _keys.Keys
            .Where(k => k.StartsWith($"{userId}:"))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private static string GetFullKeyId(string keyId, Guid userId)
    {
        return $"{userId}:{keyId}";
    }
}
