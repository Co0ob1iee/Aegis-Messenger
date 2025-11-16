using Aegis.Modules.Messages.Application.Abstractions;
using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Messages.Infrastructure.Services;

/// <summary>
/// Encryption service using Signal Protocol
/// NOTE: This is a simplified implementation for demonstration
/// In production, this would need proper key exchange and session management
/// </summary>
public class SignalProtocolEncryptionService : IEncryptionService
{
    private readonly ISignalProtocol _signalProtocol;
    private readonly ILogger<SignalProtocolEncryptionService> _logger;

    public SignalProtocolEncryptionService(
        ISignalProtocol signalProtocol,
        ILogger<SignalProtocolEncryptionService> logger)
    {
        _signalProtocol = signalProtocol;
        _logger = logger;
    }

    public async Task<(byte[] ciphertext, bool isPreKeyMessage)> EncryptMessageAsync(
        Guid senderId,
        Guid recipientId,
        string plaintext)
    {
        try
        {
            // Check if session exists
            var hasSession = await _signalProtocol.HasSessionAsync(recipientId);

            // Encrypt message
            var ciphertext = await _signalProtocol.EncryptMessageAsync(recipientId, plaintext);

            // First message is always pre-key message
            var isPreKeyMessage = !hasSession;

            _logger.LogDebug(
                "Encrypted message from {SenderId} to {RecipientId}, isPreKey: {IsPreKey}",
                senderId, recipientId, isPreKeyMessage);

            return (ciphertext, isPreKeyMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to encrypt message from {SenderId} to {RecipientId}",
                senderId, recipientId);
            throw;
        }
    }

    public async Task<string> DecryptMessageAsync(
        Guid senderId,
        Guid recipientId,
        byte[] ciphertext,
        bool isPreKeyMessage)
    {
        try
        {
            string plaintext;

            if (isPreKeyMessage)
            {
                // This is the first message - process pre-key message
                // In production, we would need the identity key pair from storage
                var identityKeyPair = await _signalProtocol.GenerateIdentityKeyPairAsync();
                plaintext = await _signalProtocol.ProcessPreKeyMessageAsync(
                    senderId,
                    ciphertext,
                    identityKeyPair);
            }
            else
            {
                // Normal message decryption
                plaintext = await _signalProtocol.DecryptMessageAsync(
                    senderId,
                    ciphertext,
                    MessageTypeDto.Normal);
            }

            _logger.LogDebug(
                "Decrypted message from {SenderId} to {RecipientId}",
                senderId, recipientId);

            return plaintext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to decrypt message from {SenderId} to {RecipientId}",
                senderId, recipientId);
            throw;
        }
    }

    public async Task<bool> InitializeSessionAsync(Guid userId1, Guid userId2)
    {
        try
        {
            // Generate identity key pairs for both users (in production, these would be stored)
            var identityKeyPair1 = await _signalProtocol.GenerateIdentityKeyPairAsync();
            var identityKeyPair2 = await _signalProtocol.GenerateIdentityKeyPairAsync();

            // Generate pre-key bundles
            var preKeyBundle2 = await _signalProtocol.GeneratePreKeyBundleAsync(
                userId2,
                identityKeyPair2,
                1);

            // Initialize session from user1 to user2
            var result = await _signalProtocol.InitializeSessionAsync(
                userId2,
                preKeyBundle2,
                identityKeyPair1);

            _logger.LogInformation(
                "Initialized encryption session between {User1} and {User2}",
                userId1, userId2);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initialize session between {User1} and {User2}",
                userId1, userId2);
            return false;
        }
    }

    public async Task<bool> HasSessionAsync(Guid userId1, Guid userId2)
    {
        return await _signalProtocol.HasSessionAsync(userId2);
    }
}
