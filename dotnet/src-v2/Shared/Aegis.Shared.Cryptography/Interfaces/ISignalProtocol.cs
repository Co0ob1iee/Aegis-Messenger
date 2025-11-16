namespace Aegis.Shared.Cryptography.Interfaces;

/// <summary>
/// Interface for Signal Protocol operations
/// Implements X3DH key agreement and Double Ratchet algorithm
/// </summary>
public interface ISignalProtocol
{
    /// <summary>
    /// Generate identity key pair for user
    /// </summary>
    Task<IdentityKeyPairDto> GenerateIdentityKeyPairAsync();

    /// <summary>
    /// Generate pre-key bundle for user
    /// </summary>
    Task<PreKeyBundleDto> GeneratePreKeyBundleAsync(
        Guid userId,
        IdentityKeyPairDto identityKeyPair,
        uint registrationId,
        uint deviceId = 1);

    /// <summary>
    /// Initialize encrypted session with recipient
    /// </summary>
    Task<bool> InitializeSessionAsync(
        Guid recipientId,
        PreKeyBundleDto preKeyBundle,
        IdentityKeyPairDto identityKeyPair);

    /// <summary>
    /// Encrypt message for recipient
    /// </summary>
    Task<byte[]> EncryptMessageAsync(Guid recipientId, string plaintext);

    /// <summary>
    /// Decrypt message from sender
    /// </summary>
    Task<string> DecryptMessageAsync(Guid senderId, byte[] ciphertext, MessageTypeDto messageType);

    /// <summary>
    /// Process pre-key message (first message in session)
    /// </summary>
    Task<string> ProcessPreKeyMessageAsync(Guid senderId, byte[] preKeyMessage, IdentityKeyPairDto identityKeyPair);

    /// <summary>
    /// Check if session exists with user
    /// </summary>
    Task<bool> HasSessionAsync(Guid userId);

    /// <summary>
    /// Delete session with user
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid userId);

    /// <summary>
    /// Generate safety number for verification
    /// </summary>
    string GenerateSafetyNumber(string localIdentityKey, string remoteIdentityKey);

    /// <summary>
    /// Verify identity key for user
    /// </summary>
    Task<bool> VerifyIdentityKeyAsync(Guid userId, string identityKey);
}

/// <summary>
/// DTO for identity key pair
/// </summary>
public record IdentityKeyPairDto(
    string PrivateKey,
    string PublicKey,
    DateTime CreatedAt
);

/// <summary>
/// DTO for pre-key bundle
/// </summary>
public record PreKeyBundleDto(
    Guid UserId,
    uint RegistrationId,
    uint DeviceId,
    uint PreKeyId,
    string PreKeyPublic,
    uint SignedPreKeyId,
    string SignedPreKeyPublic,
    string SignedPreKeySignature,
    string IdentityKey,
    DateTime CreatedAt
);

/// <summary>
/// Message type enumeration
/// </summary>
public enum MessageTypeDto
{
    Normal = 0,
    PreKey = 1
}
