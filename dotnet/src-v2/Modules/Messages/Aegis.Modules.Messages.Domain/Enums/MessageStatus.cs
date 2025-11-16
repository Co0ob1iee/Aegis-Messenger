namespace Aegis.Modules.Messages.Domain.Enums;

/// <summary>
/// Message delivery status
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message sent but not yet delivered
    /// </summary>
    Sent = 0,

    /// <summary>
    /// Message delivered to recipient's device
    /// </summary>
    Delivered = 1,

    /// <summary>
    /// Message read by recipient
    /// </summary>
    Read = 2,

    /// <summary>
    /// Message failed to send
    /// </summary>
    Failed = 3
}
