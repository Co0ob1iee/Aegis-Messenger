using System.Collections.Concurrent;
using libsignal;
using libsignal.ecc;
using libsignal.state;
using libsignal.util;

namespace Aegis.Shared.Cryptography.SignalProtocol;

/// <summary>
/// In-memory implementation of Signal Protocol store
/// WARNING: In production, this should be backed by encrypted database storage
/// This implementation loses all keys when the application restarts
/// </summary>
internal class InMemorySignalProtocolStore : SignalProtocolStore
{
    private readonly IdentityKeyPair _identityKeyPair;
    private readonly uint _localRegistrationId;
    private readonly ConcurrentDictionary<uint, PreKeyRecord> _preKeys;
    private readonly ConcurrentDictionary<uint, SignedPreKeyRecord> _signedPreKeys;
    private readonly ConcurrentDictionary<string, SessionRecord> _sessions;
    private readonly ConcurrentDictionary<string, IdentityKey> _identities;

    public InMemorySignalProtocolStore()
    {
        _identityKeyPair = KeyHelper.generateIdentityKeyPair();
        _localRegistrationId = (uint)KeyHelper.generateRegistrationId(false);
        _preKeys = new ConcurrentDictionary<uint, PreKeyRecord>();
        _signedPreKeys = new ConcurrentDictionary<uint, SignedPreKeyRecord>();
        _sessions = new ConcurrentDictionary<string, SessionRecord>();
        _identities = new ConcurrentDictionary<string, IdentityKey>();
    }

    public IdentityKeyPair GetIdentityKeyPair()
    {
        return _identityKeyPair;
    }

    public uint GetLocalRegistrationId()
    {
        return _localRegistrationId;
    }

    public bool SaveIdentity(SignalProtocolAddress address, IdentityKey identityKey)
    {
        _identities[address.getName()] = identityKey;
        return true;
    }

    public bool IsTrustedIdentity(SignalProtocolAddress address, IdentityKey identityKey, Direction direction)
    {
        if (!_identities.TryGetValue(address.getName(), out var storedKey))
            return true;

        return storedKey.Equals(identityKey);
    }

    public IdentityKey GetIdentity(SignalProtocolAddress address)
    {
        _identities.TryGetValue(address.getName(), out var identity);
        return identity;
    }

    public PreKeyRecord LoadPreKey(uint preKeyId)
    {
        if (_preKeys.TryGetValue(preKeyId, out var preKey))
            return preKey;

        throw new InvalidKeyIdException($"No pre-key with ID {preKeyId}");
    }

    public void StorePreKey(uint preKeyId, PreKeyRecord record)
    {
        _preKeys[preKeyId] = record;
    }

    public bool ContainsPreKey(uint preKeyId)
    {
        return _preKeys.ContainsKey(preKeyId);
    }

    public void RemovePreKey(uint preKeyId)
    {
        _preKeys.TryRemove(preKeyId, out _);
    }

    public SessionRecord LoadSession(SignalProtocolAddress address)
    {
        var key = $"{address.getName()}:{address.getDeviceId()}";
        if (_sessions.TryGetValue(key, out var session))
            return session;

        return new SessionRecord();
    }

    public List<uint> GetSubDeviceSessions(string name)
    {
        var devices = new List<uint>();
        foreach (var key in _sessions.Keys)
        {
            if (key.StartsWith(name + ":"))
            {
                var parts = key.Split(':');
                if (uint.TryParse(parts[1], out var deviceId))
                    devices.Add(deviceId);
            }
        }
        return devices;
    }

    public void StoreSession(SignalProtocolAddress address, SessionRecord record)
    {
        var key = $"{address.getName()}:{address.getDeviceId()}";
        _sessions[key] = record;
    }

    public bool ContainsSession(SignalProtocolAddress address)
    {
        var key = $"{address.getName()}:{address.getDeviceId()}";
        return _sessions.ContainsKey(key);
    }

    public void DeleteSession(SignalProtocolAddress address)
    {
        var key = $"{address.getName()}:{address.getDeviceId()}";
        _sessions.TryRemove(key, out _);
    }

    public void DeleteAllSessions(string name)
    {
        var keysToRemove = new List<string>();
        foreach (var key in _sessions.Keys)
        {
            if (key.StartsWith(name + ":"))
                keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove)
        {
            _sessions.TryRemove(key, out _);
        }
    }

    public SignedPreKeyRecord LoadSignedPreKey(uint signedPreKeyId)
    {
        if (_signedPreKeys.TryGetValue(signedPreKeyId, out var signedPreKey))
            return signedPreKey;

        throw new InvalidKeyIdException($"No signed pre-key with ID {signedPreKeyId}");
    }

    public List<SignedPreKeyRecord> LoadSignedPreKeys()
    {
        return new List<SignedPreKeyRecord>(_signedPreKeys.Values);
    }

    public void StoreSignedPreKey(uint signedPreKeyId, SignedPreKeyRecord record)
    {
        _signedPreKeys[signedPreKeyId] = record;
    }

    public bool ContainsSignedPreKey(uint signedPreKeyId)
    {
        return _signedPreKeys.ContainsKey(signedPreKeyId);
    }

    public void RemoveSignedPreKey(uint signedPreKeyId)
    {
        _signedPreKeys.TryRemove(signedPreKeyId, out _);
    }
}
