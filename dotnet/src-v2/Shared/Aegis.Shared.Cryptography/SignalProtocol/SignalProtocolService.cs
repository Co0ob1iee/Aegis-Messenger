using System.Collections.Concurrent;
using System.Text;
using Aegis.Shared.Cryptography.Interfaces;
using libsignal;
using libsignal.ecc;
using libsignal.protocol;
using libsignal.state;
using libsignal.util;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.SignalProtocol;

/// <summary>
/// Signal Protocol implementation for end-to-end encryption
/// Implements X3DH key agreement and Double Ratchet algorithm
/// </summary>
public class SignalProtocolService : ISignalProtocol
{
    private readonly ILogger<SignalProtocolService> _logger;
    private readonly IKeyStore _keyStore;
    private readonly ConcurrentDictionary<Guid, SessionCipher> _sessions;
    private readonly SignalProtocolStore _protocolStore;

    public SignalProtocolService(
        ILogger<SignalProtocolService> logger,
        IKeyStore keyStore)
    {
        _logger = logger;
        _keyStore = keyStore;
        _sessions = new ConcurrentDictionary<Guid, SessionCipher>();
        _protocolStore = new InMemorySignalProtocolStore();
    }

    /// <inheritdoc/>
    public async Task<IdentityKeyPairDto> GenerateIdentityKeyPairAsync()
    {
        try
        {
            var identityKeyPair = KeyHelper.generateIdentityKeyPair();

            return new IdentityKeyPairDto(
                PrivateKey: Convert.ToBase64String(identityKeyPair.getPrivateKey().serialize()),
                PublicKey: Convert.ToBase64String(identityKeyPair.getPublicKey().serialize()),
                CreatedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate identity key pair");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PreKeyBundleDto> GeneratePreKeyBundleAsync(
        Guid userId,
        IdentityKeyPairDto identityKeyPair,
        uint registrationId,
        uint deviceId = 1)
    {
        try
        {
            // Generate pre-keys
            var preKeys = KeyHelper.generatePreKeys(0, 100);
            var signedPreKey = KeyHelper.generateSignedPreKey(
                _protocolStore.GetIdentityKeyPair(),
                0);

            // Store pre-keys in protocol store
            foreach (var preKey in preKeys)
            {
                _protocolStore.StorePreKey((uint)preKey.getId(), preKey);
            }
            _protocolStore.StoreSignedPreKey((uint)signedPreKey.getId(), signedPreKey);

            // Create pre-key bundle
            var bundle = new PreKeyBundleDto(
                UserId: userId,
                RegistrationId: registrationId,
                DeviceId: deviceId,
                PreKeyId: (uint)preKeys[0].getId(),
                PreKeyPublic: Convert.ToBase64String(preKeys[0].getKeyPair().getPublicKey().serialize()),
                SignedPreKeyId: (uint)signedPreKey.getId(),
                SignedPreKeyPublic: Convert.ToBase64String(signedPreKey.getKeyPair().getPublicKey().serialize()),
                SignedPreKeySignature: Convert.ToBase64String(signedPreKey.getSignature()),
                IdentityKey: identityKeyPair.PublicKey,
                CreatedAt: DateTime.UtcNow
            );

            _logger.LogInformation("Generated pre-key bundle for user {UserId}", userId);
            return bundle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-key bundle for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> InitializeSessionAsync(
        Guid recipientId,
        PreKeyBundleDto preKeyBundle,
        IdentityKeyPairDto identityKeyPair)
    {
        try
        {
            var recipientAddress = new SignalProtocolAddress(recipientId.ToString(), preKeyBundle.DeviceId);

            // Convert PreKeyBundle to libsignal format
            var identityKey = new IdentityKey(
                Curve.decodePoint(Convert.FromBase64String(preKeyBundle.IdentityKey), 0));

            var preKeyPublic = Curve.decodePoint(
                Convert.FromBase64String(preKeyBundle.PreKeyPublic), 0);

            var signedPreKeyPublic = Curve.decodePoint(
                Convert.FromBase64String(preKeyBundle.SignedPreKeyPublic), 0);

            var signalPreKeyBundle = new libsignal.state.PreKeyBundle(
                preKeyBundle.RegistrationId,
                preKeyBundle.DeviceId,
                preKeyBundle.PreKeyId,
                preKeyPublic,
                (int)preKeyBundle.SignedPreKeyId,
                signedPreKeyPublic,
                Convert.FromBase64String(preKeyBundle.SignedPreKeySignature),
                identityKey
            );

            // Process pre-key bundle and create session
            var sessionBuilder = new SessionBuilder(_protocolStore, recipientAddress);
            sessionBuilder.process(signalPreKeyBundle);

            // Create session cipher
            var sessionCipher = new SessionCipher(_protocolStore, recipientAddress);
            _sessions.TryAdd(recipientId, sessionCipher);

            _logger.LogInformation("Initialized session with user {RecipientId}", recipientId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize session with user {RecipientId}", recipientId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> EncryptMessageAsync(Guid recipientId, string plaintext)
    {
        try
        {
            if (!_sessions.TryGetValue(recipientId, out var sessionCipher))
            {
                throw new InvalidOperationException(
                    $"No session exists with user {recipientId}. Initialize session first.");
            }

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = sessionCipher.encrypt(plaintextBytes);

            _logger.LogDebug("Encrypted message for user {RecipientId}, type: {Type}",
                recipientId, ciphertext.getType());

            return ciphertext.serialize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt message for user {RecipientId}", recipientId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> DecryptMessageAsync(
        Guid senderId,
        byte[] ciphertext,
        MessageTypeDto messageType)
    {
        try
        {
            if (messageType == MessageTypeDto.PreKey)
            {
                throw new InvalidOperationException(
                    "Use ProcessPreKeyMessageAsync for pre-key messages");
            }

            if (!_sessions.TryGetValue(senderId, out var sessionCipher))
            {
                throw new InvalidOperationException(
                    $"No session exists with user {senderId}");
            }

            var message = new SignalMessage(ciphertext);
            var plaintextBytes = sessionCipher.decrypt(message);
            var plaintext = Encoding.UTF8.GetString(plaintextBytes);

            _logger.LogDebug("Decrypted message from user {SenderId}", senderId);
            return plaintext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt message from user {SenderId}", senderId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ProcessPreKeyMessageAsync(
        Guid senderId,
        byte[] preKeyMessage,
        IdentityKeyPairDto identityKeyPair)
    {
        try
        {
            var senderAddress = new SignalProtocolAddress(senderId.ToString(), 1);

            // Create session cipher if it doesn't exist
            if (!_sessions.TryGetValue(senderId, out var sessionCipher))
            {
                sessionCipher = new SessionCipher(_protocolStore, senderAddress);
                _sessions.TryAdd(senderId, sessionCipher);
            }

            // Process pre-key message (this automatically creates the session)
            var message = new PreKeySignalMessage(preKeyMessage);
            var plaintextBytes = sessionCipher.decrypt(message);
            var plaintext = Encoding.UTF8.GetString(plaintextBytes);

            _logger.LogInformation("Processed pre-key message from user {SenderId}", senderId);
            return plaintext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process pre-key message from user {SenderId}", senderId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasSessionAsync(Guid userId)
    {
        return _sessions.ContainsKey(userId);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSessionAsync(Guid userId)
    {
        var removed = _sessions.TryRemove(userId, out _);
        if (removed)
        {
            _logger.LogInformation("Deleted session with user {UserId}", userId);
        }
        return removed;
    }

    /// <inheritdoc/>
    public string GenerateSafetyNumber(string localIdentityKey, string remoteIdentityKey)
    {
        try
        {
            // Concatenate both identity keys
            var combined = localIdentityKey + remoteIdentityKey;
            var hash = System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(combined));

            // Format as groups of 5 digits
            var safetyNumber = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                if (i > 0 && i % 2 == 0)
                    safetyNumber.Append(' ');

                var value = BitConverter.ToUInt16(hash, i * 2) % 100000;
                safetyNumber.Append(value.ToString("D5"));
            }

            return safetyNumber.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate safety number");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyIdentityKeyAsync(Guid userId, string identityKey)
    {
        try
        {
            // Get stored identity key for this user
            var storedKey = _protocolStore.GetIdentity(
                new SignalProtocolAddress(userId.ToString(), 1));

            if (storedKey == null)
                return false;

            var storedKeyBase64 = Convert.ToBase64String(storedKey.getPublicKey().serialize());
            return storedKeyBase64 == identityKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify identity key for user {UserId}", userId);
            return false;
        }
    }
}
