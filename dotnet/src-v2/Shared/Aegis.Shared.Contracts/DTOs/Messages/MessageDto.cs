namespace Aegis.Shared.Contracts.DTOs.Messages;

/// <summary>
/// Data transfer object for a message
/// </summary>
/// <param name="Id">Unique message identifier</param>
/// <param name="SenderId">User ID who sent the message</param>
/// <param name="RecipientId">User ID who receives the message</param>
/// <param name="EncryptedContent">Encrypted message content</param>
/// <param name="SentAt">Timestamp when message was sent</param>
/// <param name="DeliveredAt">Timestamp when message was delivered (nullable)</param>
/// <param name="ReadAt">Timestamp when message was read (nullable)</param>
/// <param name="IsGroup">Whether this is a group message</param>
/// <param name="GroupId">Group ID if this is a group message</param>
public record MessageDto(
    Guid Id,
    Guid SenderId,
    Guid RecipientId,
    byte[] EncryptedContent,
    DateTime SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    bool IsGroup,
    Guid? GroupId
);
