namespace Aegis.Shared.Cryptography.SealedSender;

/// <summary>
/// Service for sealed sender (anonymous sender) message encryption and decryption.
/// Sealed sender hides the sender's identity from the server while still allowing the recipient to verify authenticity.
/// </summary>
public interface ISealedSenderService
{
    /// <summary>
    /// Encrypt a message using sealed sender protocol
    /// The server will only see the recipient, not the sender
    /// </summary>
    /// <param name="recipientId">Recipient user ID</param>
    /// <param name="recipientIdentityKey">Recipient's public identity key</param>
    /// <param name="plaintext">Message to encrypt</param>
    /// <param name="senderCertificate">Sender's certificate (proves authorization)</param>
    /// <param name="signalProtocolPayload">Already encrypted Signal Protocol payload</param>
    /// <returns>Sealed sender message envelope</returns>
    Task<UnidentifiedSenderMessage> EncryptAsync(
        Guid recipientId,
        string recipientIdentityKey,
        string plaintext,
        SenderCertificate senderCertificate,
        byte[] signalProtocolPayload);

    /// <summary>
    /// Decrypt a sealed sender message
    /// Extracts sender ID from certificate and decrypts the payload
    /// </summary>
    /// <param name="sealedMessage">Sealed sender message to decrypt</param>
    /// <param name="recipientPrivateKey">Recipient's private identity key</param>
    /// <param name="serverPublicKey">Server's public key (to verify certificate signature)</param>
    /// <returns>Decryption result with sender ID and plaintext</returns>
    Task<SealedSenderDecryptionResult> DecryptAsync(
        UnidentifiedSenderMessage sealedMessage,
        string recipientPrivateKey,
        string serverPublicKey);

    /// <summary>
    /// Create a sealed sender message from plaintext (complete encryption pipeline)
    /// </summary>
    /// <param name="senderId">Sender user ID</param>
    /// <param name="recipientId">Recipient user ID</param>
    /// <param name="plaintext">Message to send</param>
    /// <param name="senderCertificate">Sender's certificate</param>
    /// <returns>Complete sealed sender message ready for transmission</returns>
    Task<UnidentifiedSenderMessage> CreateSealedMessageAsync(
        Guid senderId,
        Guid recipientId,
        string plaintext,
        SenderCertificate senderCertificate);

    /// <summary>
    /// Decrypt and process a complete sealed sender message
    /// </summary>
    /// <param name="recipientId">Recipient user ID (us)</param>
    /// <param name="sealedMessage">Sealed message to decrypt</param>
    /// <returns>Decrypted message with sender information</returns>
    Task<SealedSenderDecryptionResult> ProcessSealedMessageAsync(
        Guid recipientId,
        UnidentifiedSenderMessage sealedMessage);
}

/// <summary>
/// Service for sender certificate management
/// </summary>
public interface ISenderCertificateService
{
    /// <summary>
    /// Generate a new sender certificate for a user
    /// Typically called by the server when user requests to send a sealed sender message
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="identityKey">User's public identity key</param>
    /// <param name="validityPeriod">How long the certificate is valid (default: 24 hours)</param>
    /// <returns>Signed sender certificate</returns>
    Task<SenderCertificate> GenerateCertificateAsync(
        Guid userId,
        uint deviceId,
        string identityKey,
        TimeSpan? validityPeriod = null);

    /// <summary>
    /// Verify a sender certificate
    /// </summary>
    /// <param name="certificate">Certificate to verify</param>
    /// <param name="serverPublicKey">Server's public key</param>
    /// <returns>True if certificate is valid and not expired</returns>
    Task<bool> VerifyCertificateAsync(SenderCertificate certificate, string serverPublicKey);

    /// <summary>
    /// Get or create a sender certificate for a user
    /// Uses cached certificate if available and not expired
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="identityKey">User's public identity key</param>
    /// <returns>Valid sender certificate</returns>
    Task<SenderCertificate> GetOrCreateCertificateAsync(
        Guid userId,
        uint deviceId,
        string identityKey);

    /// <summary>
    /// Revoke a sender certificate (e.g., when user's key changes)
    /// </summary>
    /// <param name="certificateId">Certificate ID to revoke</param>
    Task RevokeCertificateAsync(Guid certificateId);
}
