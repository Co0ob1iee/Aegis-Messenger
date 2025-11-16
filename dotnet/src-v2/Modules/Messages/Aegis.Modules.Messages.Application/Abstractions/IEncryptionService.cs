namespace Aegis.Modules.Messages.Application.Abstractions;

/// <summary>
/// Service for encrypting and decrypting messages using Signal Protocol
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypt message for recipient
    /// </summary>
    /// <param name="senderId">Sender user ID</param>
    /// <param name="recipientId">Recipient user ID</param>
    /// <param name="plaintext">Plain text message</param>
    /// <returns>Encrypted content with pre-key flag</returns>
    Task<(byte[] ciphertext, bool isPreKeyMessage)> EncryptMessageAsync(
        Guid senderId,
        Guid recipientId,
        string plaintext);

    /// <summary>
    /// Decrypt message from sender
    /// </summary>
    /// <param name="senderId">Sender user ID</param>
    /// <param name="recipientId">Recipient user ID (current user)</param>
    /// <param name="ciphertext">Encrypted content</param>
    /// <param name="isPreKeyMessage">Whether this is a pre-key message</param>
    /// <returns>Decrypted plain text</returns>
    Task<string> DecryptMessageAsync(
        Guid senderId,
        Guid recipientId,
        byte[] ciphertext,
        bool isPreKeyMessage);

    /// <summary>
    /// Initialize encryption session with user
    /// </summary>
    Task<bool> InitializeSessionAsync(Guid userId1, Guid userId2);

    /// <summary>
    /// Check if encryption session exists
    /// </summary>
    Task<bool> HasSessionAsync(Guid userId1, Guid userId2);
}
