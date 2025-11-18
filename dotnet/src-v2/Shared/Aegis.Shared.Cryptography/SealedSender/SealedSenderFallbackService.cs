using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.SealedSender;

/// <summary>
/// Fallback service that tries sealed sender first, then falls back to normal Signal Protocol
/// This ensures messages are delivered even if sealed sender fails
/// </summary>
public class SealedSenderFallbackService
{
    private readonly ILogger<SealedSenderFallbackService> _logger;
    private readonly ISealedSenderService _sealedSenderService;
    private readonly ISignalProtocol _signalProtocol;
    private readonly ISenderCertificateService _certificateService;

    public SealedSenderFallbackService(
        ILogger<SealedSenderFallbackService> logger,
        ISealedSenderService sealedSenderService,
        ISignalProtocol signalProtocol,
        ISenderCertificateService certificateService)
    {
        _logger = logger;
        _sealedSenderService = sealedSenderService;
        _signalProtocol = signalProtocol;
        _certificateService = certificateService;
    }

    /// <summary>
    /// Send message with sealed sender, fallback to normal if it fails
    /// </summary>
    /// <param name="senderId">Sender ID</param>
    /// <param name="recipientId">Recipient ID</param>
    /// <param name="plaintext">Message to send</param>
    /// <param name="senderIdentityKey">Sender's public identity key</param>
    /// <param name="useSealedSender">Whether to attempt sealed sender first (default: true)</param>
    /// <returns>Encrypted message and whether sealed sender was used</returns>
    public async Task<(byte[] encryptedMessage, bool usedSealedSender)> SendMessageAsync(
        Guid senderId,
        Guid recipientId,
        string plaintext,
        string senderIdentityKey,
        bool useSealedSender = true)
    {
        if (useSealedSender)
        {
            try
            {
                _logger.LogDebug(
                    "Attempting to send sealed sender message from {SenderId} to {RecipientId}",
                    senderId, recipientId);

                // 1. Get or create sender certificate
                var certificate = await _certificateService.GetOrCreateCertificateAsync(
                    senderId,
                    deviceId: 1,
                    senderIdentityKey);

                // 2. Create sealed sender message
                var sealedMessage = await _sealedSenderService.CreateSealedMessageAsync(
                    senderId,
                    recipientId,
                    plaintext,
                    certificate);

                // 3. Serialize sealed message
                var encryptedMessage = sealedMessage.Serialize();

                _logger.LogInformation(
                    "Successfully sent sealed sender message from {SenderId} to {RecipientId}",
                    senderId, recipientId);

                return (encryptedMessage, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Sealed sender failed for {SenderId} to {RecipientId}, falling back to normal",
                    senderId, recipientId);

                // Fall through to normal encryption
            }
        }

        // Fallback: use normal Signal Protocol
        _logger.LogDebug(
            "Sending normal Signal Protocol message from {SenderId} to {RecipientId}",
            senderId, recipientId);

        var normalMessage = await _signalProtocol.EncryptMessageAsync(recipientId, plaintext);

        _logger.LogInformation(
            "Successfully sent normal message from {SenderId} to {RecipientId}",
            senderId, recipientId);

        return (normalMessage, false);
    }

    /// <summary>
    /// Receive message - automatically detects sealed sender vs normal
    /// </summary>
    /// <param name="recipientId">Recipient ID (us)</param>
    /// <param name="encryptedMessage">Encrypted message bytes</param>
    /// <param name="senderId">Sender ID (only needed for normal messages, null for sealed sender)</param>
    /// <param name="messageType">Message type (only needed for normal messages)</param>
    /// <returns>Decrypted plaintext and actual sender ID</returns>
    public async Task<(string plaintext, Guid senderId)> ReceiveMessageAsync(
        Guid recipientId,
        byte[] encryptedMessage,
        Guid? senderId = null,
        MessageTypeDto? messageType = null)
    {
        // Try to detect if this is a sealed sender message
        if (IsSealedSenderMessage(encryptedMessage))
        {
            try
            {
                _logger.LogDebug(
                    "Attempting to decrypt as sealed sender message for recipient {RecipientId}",
                    recipientId);

                // Deserialize sealed sender message
                var sealedMessage = UnidentifiedSenderMessage.Deserialize(encryptedMessage);

                // Decrypt sealed sender message
                var result = await _sealedSenderService.ProcessSealedMessageAsync(
                    recipientId,
                    sealedMessage);

                _logger.LogInformation(
                    "Successfully decrypted sealed sender message from {SenderId} for recipient {RecipientId}",
                    result.SenderId, recipientId);

                return (result.Plaintext, result.SenderId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to decrypt as sealed sender message for recipient {RecipientId}",
                    recipientId);

                // If sender ID is provided, try as normal message
                if (senderId.HasValue && messageType.HasValue)
                {
                    _logger.LogDebug("Falling back to normal decryption");
                    goto NormalDecryption;
                }

                throw;
            }
        }

    NormalDecryption:
        // Normal Signal Protocol message
        if (!senderId.HasValue || !messageType.HasValue)
        {
            throw new InvalidOperationException(
                "Sender ID and message type are required for normal (non-sealed) messages");
        }

        _logger.LogDebug(
            "Decrypting as normal Signal Protocol message from {SenderId} for recipient {RecipientId}",
            senderId.Value, recipientId);

        var plaintext = await _signalProtocol.DecryptMessageAsync(
            senderId.Value,
            encryptedMessage,
            messageType.Value);

        _logger.LogInformation(
            "Successfully decrypted normal message from {SenderId} for recipient {RecipientId}",
            senderId.Value, recipientId);

        return (plaintext, senderId.Value);
    }

    /// <summary>
    /// Detect if message is sealed sender format
    /// Sealed sender messages start with version byte (0x01)
    /// </summary>
    private bool IsSealedSenderMessage(byte[] encryptedMessage)
    {
        if (encryptedMessage == null || encryptedMessage.Length < 1)
            return false;

        // Check for sealed sender version byte
        // Version 1 = 0x01
        return encryptedMessage[0] == 0x01;
    }
}

/// <summary>
/// Configuration options for sealed sender
/// </summary>
public class SealedSenderOptions
{
    /// <summary>
    /// Whether sealed sender is enabled globally (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to fallback to normal messages when sealed sender fails (default: true)
    /// </summary>
    public bool AllowFallback { get; set; } = true;

    /// <summary>
    /// Certificate validity period (default: 24 hours)
    /// </summary>
    public TimeSpan CertificateValidityPeriod { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Certificate renewal threshold - renew certificate when it has less than this time remaining
    /// (default: 6 hours before expiration)
    /// </summary>
    public TimeSpan CertificateRenewalThreshold { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Maximum number of cached certificates (default: 10000)
    /// </summary>
    public int MaxCachedCertificates { get; set; } = 10000;

    /// <summary>
    /// How often to cleanup expired certificates (default: 1 hour)
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
