namespace Aegis.Shared.Cryptography.SealedSender;

/// <summary>
/// Unidentified Sender Message (Sealed Sender envelope)
/// Multi-layer encrypted message that hides the sender's identity from the server.
///
/// Structure:
/// 1. Outer envelope - encrypted with recipient's ephemeral key (server cannot decrypt)
/// 2. Inner content - contains sender certificate + Signal Protocol encrypted payload
/// 3. Encrypted payload - normal Signal Protocol encryption
///
/// The server only sees the recipient ID, not the sender ID.
/// </summary>
public class UnidentifiedSenderMessage
{
    /// <summary>
    /// Version of the sealed sender protocol (for future compatibility)
    /// </summary>
    public byte Version { get; set; } = 1;

    /// <summary>
    /// Ephemeral public key used for outer envelope encryption
    /// Generated fresh for each message
    /// </summary>
    public byte[] EphemeralPublicKey { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Encrypted inner content (contains sender certificate + encrypted payload)
    /// Encrypted using AEAD (AES-256-GCM) with key derived from ephemeral key exchange
    /// </summary>
    public byte[] EncryptedContent { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Authentication tag for AEAD encryption (96 bits / 12 bytes)
    /// </summary>
    public byte[] AuthenticationTag { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Serialize to bytes for transmission
    /// Format: [Version:1][EphemeralKeyLen:2][EphemeralKey][TagLen:1][Tag][Content]
    /// </summary>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version
        writer.Write(Version);

        // Ephemeral public key
        writer.Write((ushort)EphemeralPublicKey.Length);
        writer.Write(EphemeralPublicKey);

        // Authentication tag
        writer.Write((byte)AuthenticationTag.Length);
        writer.Write(AuthenticationTag);

        // Encrypted content
        writer.Write(EncryptedContent);

        return ms.ToArray();
    }

    /// <summary>
    /// Deserialize from bytes
    /// </summary>
    public static UnidentifiedSenderMessage Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var message = new UnidentifiedSenderMessage
        {
            Version = reader.ReadByte()
        };

        // Read ephemeral public key
        var ephemeralKeyLen = reader.ReadUInt16();
        message.EphemeralPublicKey = reader.ReadBytes(ephemeralKeyLen);

        // Read authentication tag
        var tagLen = reader.ReadByte();
        message.AuthenticationTag = reader.ReadBytes(tagLen);

        // Read encrypted content (rest of the data)
        var contentLen = (int)(ms.Length - ms.Position);
        message.EncryptedContent = reader.ReadBytes(contentLen);

        return message;
    }
}

/// <summary>
/// Inner content of unidentified sender message (before outer encryption)
/// </summary>
public class UnidentifiedSenderMessageContent
{
    /// <summary>
    /// Sender certificate (proves sender is authorized)
    /// </summary>
    public SenderCertificate SenderCertificate { get; set; } = new();

    /// <summary>
    /// Signal Protocol encrypted payload
    /// This is the normal encrypted message
    /// </summary>
    public byte[] EncryptedPayload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Message type (PreKey or Normal)
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Serialize to bytes
    /// </summary>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Message type
        writer.Write((byte)Type);

        // Sender certificate
        var certBytes = SenderCertificate.Serialize();
        writer.Write(certBytes.Length);
        writer.Write(certBytes);

        // Server signature
        var sigBytes = Convert.FromBase64String(SenderCertificate.ServerSignature);
        writer.Write((ushort)sigBytes.Length);
        writer.Write(sigBytes);

        // Encrypted payload
        writer.Write(EncryptedPayload);

        return ms.ToArray();
    }

    /// <summary>
    /// Deserialize from bytes
    /// </summary>
    public static UnidentifiedSenderMessageContent Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var content = new UnidentifiedSenderMessageContent
        {
            Type = (MessageType)reader.ReadByte()
        };

        // Read certificate
        var certLen = reader.ReadInt32();
        var certBytes = reader.ReadBytes(certLen);

        // Deserialize certificate (simplified - in production use proper deserialization)
        var certJson = System.Text.Encoding.UTF8.GetString(certBytes);
        var certData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(certJson);

        content.SenderCertificate = new SenderCertificate
        {
            SenderId = Guid.Parse(certData!["SenderId"]),
            DeviceId = uint.Parse(certData["DeviceId"]),
            SenderIdentityKey = certData["SenderIdentityKey"],
            ExpiresAt = DateTime.Parse(certData["ExpiresAt"])
        };

        // Read signature
        var sigLen = reader.ReadUInt16();
        var sigBytes = reader.ReadBytes(sigLen);
        content.SenderCertificate.ServerSignature = Convert.ToBase64String(sigBytes);

        // Read encrypted payload
        var payloadLen = (int)(ms.Length - ms.Position);
        content.EncryptedPayload = reader.ReadBytes(payloadLen);

        return content;
    }
}

/// <summary>
/// Message type for sealed sender
/// </summary>
public enum MessageType : byte
{
    /// <summary>
    /// Normal message (existing session)
    /// </summary>
    Normal = 0,

    /// <summary>
    /// PreKey message (first message, establishes session)
    /// </summary>
    PreKey = 1
}

/// <summary>
/// Result of decrypting a sealed sender message
/// </summary>
public record SealedSenderDecryptionResult(
    /// <summary>
    /// Sender ID (extracted from certificate)
    /// </summary>
    Guid SenderId,

    /// <summary>
    /// Device ID
    /// </summary>
    uint DeviceId,

    /// <summary>
    /// Decrypted plaintext message
    /// </summary>
    string Plaintext,

    /// <summary>
    /// Sender's identity key (for verification)
    /// </summary>
    string SenderIdentityKey
);
