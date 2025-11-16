using System;

namespace Aegis.Core.Models;

/// <summary>
/// Represents an encrypted message in the Aegis Messenger system.
/// Messages are encrypted using Signal Protocol (Double Ratchet).
/// </summary>
public class Message
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the sender (user who sent the message)
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// ID of the receiver (user or group receiving the message)
    /// </summary>
    public Guid ReceiverId { get; set; }

    /// <summary>
    /// Encrypted message content (ciphertext)
    /// Signal Protocol encryption produces byte array
    /// </summary>
    public byte[] EncryptedContent { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Message type for Signal Protocol (PreKeyMessage or regular Message)
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Regular;

    /// <summary>
    /// Timestamp when the message was created (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Flag indicating if this is a group message
    /// </summary>
    public bool IsGroup { get; set; }

    /// <summary>
    /// Group ID if IsGroup is true
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Message status (sent, delivered, read)
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    /// <summary>
    /// Flag for sealed sender (sender anonymity)
    /// If true, server doesn't know who sent the message
    /// </summary>
    public bool IsSealedSender { get; set; }

    /// <summary>
    /// Optional file attachment ID
    /// </summary>
    public Guid? FileAttachmentId { get; set; }

    /// <summary>
    /// Server-assigned message ID (for synchronization)
    /// </summary>
    public long? ServerMessageId { get; set; }
}

/// <summary>
/// Message type enumeration for Signal Protocol
/// </summary>
public enum MessageType
{
    /// <summary>
    /// First message in a session (contains pre-key bundle)
    /// </summary>
    PreKey = 1,

    /// <summary>
    /// Regular message (uses existing session)
    /// </summary>
    Regular = 2,

    /// <summary>
    /// Group message (encrypted with sender key)
    /// </summary>
    GroupMessage = 3
}

/// <summary>
/// Message delivery status
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message created but not sent to server
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message sent to server
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Message delivered to recipient's device
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Message read by recipient
    /// </summary>
    Read = 3,

    /// <summary>
    /// Message failed to send
    /// </summary>
    Failed = 4
}
