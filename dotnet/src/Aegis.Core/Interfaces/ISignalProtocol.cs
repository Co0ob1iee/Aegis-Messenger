using System;
using System.Threading.Tasks;
using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

/// <summary>
/// Interface for Signal Protocol encryption and decryption
/// Implements X3DH key agreement and Double Ratchet algorithm
/// </summary>
public interface ISignalProtocol
{
    /// <summary>
    /// Generate identity key pair (long-term key)
    /// </summary>
    /// <returns>Identity key pair</returns>
    Task<IdentityKeyPair> GenerateIdentityKeyPairAsync();

    /// <summary>
    /// Generate pre-key bundle for publishing to server
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="identityKeyPair">User's identity key pair</param>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Pre-key bundle ready to upload</returns>
    Task<PreKeyBundle> GeneratePreKeyBundleAsync(
        Guid userId,
        IdentityKeyPair identityKeyPair,
        uint registrationId,
        uint deviceId = 1);

    /// <summary>
    /// Initialize a new Signal session with a contact using their pre-key bundle
    /// This performs X3DH key agreement
    /// </summary>
    /// <param name="recipientId">Recipient user ID</param>
    /// <param name="preKeyBundle">Recipient's pre-key bundle</param>
    /// <param name="identityKeyPair">Local identity key pair</param>
    /// <returns>True if session initialized successfully</returns>
    Task<bool> InitializeSessionAsync(
        Guid recipientId,
        PreKeyBundle preKeyBundle,
        IdentityKeyPair identityKeyPair);

    /// <summary>
    /// Encrypt a message using Signal Protocol (Double Ratchet)
    /// </summary>
    /// <param name="recipientId">Recipient user ID</param>
    /// <param name="plaintext">Plaintext message</param>
    /// <returns>Encrypted message as byte array</returns>
    Task<byte[]> EncryptMessageAsync(Guid recipientId, string plaintext);

    /// <summary>
    /// Decrypt a message using Signal Protocol
    /// </summary>
    /// <param name="senderId">Sender user ID</param>
    /// <param name="ciphertext">Encrypted message</param>
    /// <param name="messageType">Message type (PreKey or Regular)</param>
    /// <returns>Decrypted plaintext</returns>
    Task<string> DecryptMessageAsync(
        Guid senderId,
        byte[] ciphertext,
        MessageType messageType);

    /// <summary>
    /// Process pre-key message (first message in a session)
    /// This establishes the session and decrypts the message
    /// </summary>
    /// <param name="senderId">Sender user ID</param>
    /// <param name="preKeyMessage">Pre-key message ciphertext</param>
    /// <param name="identityKeyPair">Local identity key pair</param>
    /// <returns>Decrypted plaintext</returns>
    Task<string> ProcessPreKeyMessageAsync(
        Guid senderId,
        byte[] preKeyMessage,
        IdentityKeyPair identityKeyPair);

    /// <summary>
    /// Check if a session exists with a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if session exists</returns>
    Task<bool> HasSessionAsync(Guid userId);

    /// <summary>
    /// Delete a session with a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteSessionAsync(Guid userId);

    /// <summary>
    /// Get safety number for identity verification
    /// Safety number is a fingerprint of both users' identity keys
    /// </summary>
    /// <param name="localIdentityKey">Local identity key</param>
    /// <param name="remoteIdentityKey">Remote identity key</param>
    /// <returns>Safety number (formatted string)</returns>
    string GenerateSafetyNumber(string localIdentityKey, string remoteIdentityKey);

    /// <summary>
    /// Verify identity key of a contact (for MITM detection)
    /// </summary>
    /// <param name="userId">Contact user ID</param>
    /// <param name="identityKey">Expected identity key</param>
    /// <returns>True if identity key matches</returns>
    Task<bool> VerifyIdentityKeyAsync(Guid userId, string identityKey);
}
