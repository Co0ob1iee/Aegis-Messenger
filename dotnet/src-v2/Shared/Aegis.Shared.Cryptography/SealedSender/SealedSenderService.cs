using System.Security.Cryptography;
using System.Text;
using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.SealedSender;

/// <summary>
/// Service for sealed sender message encryption and decryption
/// Implements multi-layer encryption to hide sender identity from the server
/// </summary>
public class SealedSenderService : ISealedSenderService
{
    private readonly ILogger<SealedSenderService> _logger;
    private readonly ISignalProtocol _signalProtocol;
    private readonly ISenderCertificateService _certificateService;

    public SealedSenderService(
        ILogger<SealedSenderService> logger,
        ISignalProtocol signalProtocol,
        ISenderCertificateService certificateService)
    {
        _logger = logger;
        _signalProtocol = signalProtocol;
        _certificateService = certificateService;
    }

    /// <inheritdoc/>
    public async Task<UnidentifiedSenderMessage> EncryptAsync(
        Guid recipientId,
        string recipientIdentityKey,
        string plaintext,
        SenderCertificate senderCertificate,
        byte[] signalProtocolPayload)
    {
        try
        {
            // 1. Create inner content (certificate + encrypted payload)
            var innerContent = new UnidentifiedSenderMessageContent
            {
                SenderCertificate = senderCertificate,
                EncryptedPayload = signalProtocolPayload,
                Type = MessageType.Normal
            };

            var innerContentBytes = innerContent.Serialize();

            // 2. Generate ephemeral key pair for outer envelope encryption
            using var ephemeralKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var ephemeralPublicKey = ephemeralKey.PublicKey.ExportSubjectPublicKeyInfo();

            // 3. Derive shared secret using ECDH
            var recipientPublicKeyBytes = Convert.FromBase64String(recipientIdentityKey);
            using var recipientECDH = ECDiffieHellman.Create();
            recipientECDH.ImportSubjectPublicKeyInfo(recipientPublicKeyBytes, out _);

            var sharedSecret = ephemeralKey.DeriveKeyMaterial(recipientECDH.PublicKey);

            // 4. Derive encryption key using HKDF
            var encryptionKey = DeriveKey(sharedSecret, "SealedSenderEncryption", 32);
            var nonce = RandomNumberGenerator.GetBytes(12); // 96-bit nonce for AES-GCM

            // 5. Encrypt inner content with AES-256-GCM
            using var aesGcm = new AesGcm(encryptionKey);
            var ciphertext = new byte[innerContentBytes.Length];
            var tag = new byte[16]; // 128-bit authentication tag

            aesGcm.Encrypt(nonce, innerContentBytes, ciphertext, tag);

            // 6. Create sealed sender message
            var sealedMessage = new UnidentifiedSenderMessage
            {
                Version = 1,
                EphemeralPublicKey = ephemeralPublicKey,
                EncryptedContent = CombineNonceAndCiphertext(nonce, ciphertext),
                AuthenticationTag = tag
            };

            _logger.LogDebug(
                "Encrypted sealed sender message for recipient {RecipientId}",
                recipientId);

            return sealedMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt sealed sender message");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SealedSenderDecryptionResult> DecryptAsync(
        UnidentifiedSenderMessage sealedMessage,
        string recipientPrivateKey,
        string serverPublicKey)
    {
        try
        {
            // 1. Derive shared secret using ECDH with ephemeral public key
            using var recipientECDH = ECDiffieHellman.Create();
            recipientECDH.ImportECPrivateKey(Convert.FromBase64String(recipientPrivateKey), out _);

            using var ephemeralPublicKeyECDH = ECDiffieHellman.Create();
            ephemeralPublicKeyECDH.ImportSubjectPublicKeyInfo(sealedMessage.EphemeralPublicKey, out _);

            var sharedSecret = recipientECDH.DeriveKeyMaterial(ephemeralPublicKeyECDH.PublicKey);

            // 2. Derive decryption key using HKDF
            var decryptionKey = DeriveKey(sharedSecret, "SealedSenderEncryption", 32);

            // 3. Split nonce and ciphertext
            var (nonce, ciphertext) = SplitNonceAndCiphertext(sealedMessage.EncryptedContent);

            // 4. Decrypt inner content with AES-256-GCM
            using var aesGcm = new AesGcm(decryptionKey);
            var plaintext = new byte[ciphertext.Length];

            aesGcm.Decrypt(nonce, ciphertext, sealedMessage.AuthenticationTag, plaintext);

            // 5. Deserialize inner content
            var innerContent = UnidentifiedSenderMessageContent.Deserialize(plaintext);

            // 6. Verify sender certificate
            var isValidCert = await _certificateService.VerifyCertificateAsync(
                innerContent.SenderCertificate,
                serverPublicKey);

            if (!isValidCert)
            {
                throw new InvalidOperationException("Sender certificate is invalid or expired");
            }

            // 7. Decrypt Signal Protocol payload
            var senderId = innerContent.SenderCertificate.SenderId;
            var decryptedMessage = await _signalProtocol.DecryptMessageAsync(
                senderId,
                innerContent.EncryptedPayload,
                innerContent.Type == MessageType.PreKey
                    ? MessageTypeDto.PreKey
                    : MessageTypeDto.Normal);

            var result = new SealedSenderDecryptionResult(
                SenderId: senderId,
                DeviceId: innerContent.SenderCertificate.DeviceId,
                Plaintext: decryptedMessage,
                SenderIdentityKey: innerContent.SenderCertificate.SenderIdentityKey
            );

            _logger.LogDebug(
                "Decrypted sealed sender message from sender {SenderId}",
                senderId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt sealed sender message");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<UnidentifiedSenderMessage> CreateSealedMessageAsync(
        Guid senderId,
        Guid recipientId,
        string plaintext,
        SenderCertificate senderCertificate)
    {
        try
        {
            // 1. Encrypt with Signal Protocol
            var signalPayload = await _signalProtocol.EncryptMessageAsync(recipientId, plaintext);

            // 2. Get recipient's identity key (in production, fetch from server/storage)
            // For now, using placeholder - this should be fetched from user registry
            var recipientIdentityKey = "RECIPIENT_IDENTITY_KEY_PLACEHOLDER";

            // 3. Encrypt with sealed sender
            var sealedMessage = await EncryptAsync(
                recipientId,
                recipientIdentityKey,
                plaintext,
                senderCertificate,
                signalPayload);

            _logger.LogInformation(
                "Created sealed sender message from {SenderId} to {RecipientId}",
                senderId, recipientId);

            return sealedMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create sealed sender message from {SenderId} to {RecipientId}",
                senderId, recipientId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SealedSenderDecryptionResult> ProcessSealedMessageAsync(
        Guid recipientId,
        UnidentifiedSenderMessage sealedMessage)
    {
        try
        {
            // 1. Get recipient's private key (in production, fetch from secure storage)
            // For now, using placeholder
            var recipientPrivateKey = "RECIPIENT_PRIVATE_KEY_PLACEHOLDER";

            // 2. Get server's public key
            var serverPublicKey = "SERVER_PUBLIC_KEY_PLACEHOLDER";

            // 3. Decrypt sealed message
            var result = await DecryptAsync(sealedMessage, recipientPrivateKey, serverPublicKey);

            _logger.LogInformation(
                "Processed sealed sender message for recipient {RecipientId} from sender {SenderId}",
                recipientId, result.SenderId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process sealed sender message for recipient {RecipientId}",
                recipientId);
            throw;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Derive key using HKDF (HMAC-based Key Derivation Function)
    /// </summary>
    private byte[] DeriveKey(byte[] inputKeyMaterial, string info, int outputLength)
    {
        var salt = Encoding.UTF8.GetBytes("AegisMessengerSealedSender");
        var infoBytes = Encoding.UTF8.GetBytes(info);

        using var hkdf = new HKDF(HashAlgorithmName.SHA256, inputKeyMaterial, salt);
        var output = new byte[outputLength];
        hkdf.DeriveKey(infoBytes, output);
        return output;
    }

    /// <summary>
    /// Combine nonce and ciphertext for storage
    /// </summary>
    private byte[] CombineNonceAndCiphertext(byte[] nonce, byte[] ciphertext)
    {
        var combined = new byte[nonce.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length, ciphertext.Length);
        return combined;
    }

    /// <summary>
    /// Split nonce and ciphertext
    /// </summary>
    private (byte[] nonce, byte[] ciphertext) SplitNonceAndCiphertext(byte[] combined)
    {
        const int nonceLength = 12; // 96-bit nonce for AES-GCM
        var nonce = new byte[nonceLength];
        var ciphertext = new byte[combined.Length - nonceLength];

        Buffer.BlockCopy(combined, 0, nonce, 0, nonceLength);
        Buffer.BlockCopy(combined, nonceLength, ciphertext, 0, ciphertext.Length);

        return (nonce, ciphertext);
    }

    #endregion
}

/// <summary>
/// HKDF implementation for key derivation
/// </summary>
internal class HKDF : IDisposable
{
    private readonly HashAlgorithmName _hashAlgorithm;
    private readonly byte[] _prk;
    private bool _disposed;

    public HKDF(HashAlgorithmName hashAlgorithm, byte[] inputKeyMaterial, byte[]? salt = null)
    {
        _hashAlgorithm = hashAlgorithm;

        // HKDF-Extract: PRK = HMAC-Hash(salt, IKM)
        using var hmac = IncrementalHash.CreateHMAC(_hashAlgorithm, salt ?? Array.Empty<byte>());
        hmac.AppendData(inputKeyMaterial);
        _prk = hmac.GetHashAndReset();
    }

    /// <summary>
    /// HKDF-Expand: derive output keying material
    /// </summary>
    public void DeriveKey(byte[] info, Span<byte> output)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HKDF));

        var hashLength = GetHashLength(_hashAlgorithm);
        var iterations = (output.Length + hashLength - 1) / hashLength;

        if (iterations > 255)
            throw new ArgumentException("Output length too long for HKDF");

        using var hmac = IncrementalHash.CreateHMAC(_hashAlgorithm, _prk);
        var t = Array.Empty<byte>();
        var pos = 0;

        for (byte i = 1; i <= iterations; i++)
        {
            hmac.AppendData(t);
            hmac.AppendData(info);
            hmac.AppendData(new[] { i });

            t = hmac.GetHashAndReset();
            var toCopy = Math.Min(t.Length, output.Length - pos);
            t.AsSpan(0, toCopy).CopyTo(output.Slice(pos, toCopy));
            pos += toCopy;
        }
    }

    private static int GetHashLength(HashAlgorithmName algorithm)
    {
        if (algorithm == HashAlgorithmName.SHA256) return 32;
        if (algorithm == HashAlgorithmName.SHA384) return 48;
        if (algorithm == HashAlgorithmName.SHA512) return 64;
        throw new ArgumentException($"Unsupported hash algorithm: {algorithm}");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Array.Clear(_prk, 0, _prk.Length);
            _disposed = true;
        }
    }
}
