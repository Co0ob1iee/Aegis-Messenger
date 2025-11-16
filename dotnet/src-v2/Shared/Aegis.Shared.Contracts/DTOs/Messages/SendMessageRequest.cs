namespace Aegis.Shared.Contracts.DTOs.Messages;

/// <summary>
/// Request to send an encrypted message
/// </summary>
/// <param name="RecipientId">User ID of the recipient</param>
/// <param name="EncryptedContent">Message content encrypted with Signal Protocol</param>
/// <param name="IsGroup">Whether this is a group message</param>
/// <param name="GroupId">Group ID if this is a group message</param>
/// <param name="ReplyToMessageId">ID of message being replied to (optional)</param>
public record SendMessageRequest(
    Guid RecipientId,
    byte[] EncryptedContent,
    bool IsGroup = false,
    Guid? GroupId = null,
    Guid? ReplyToMessageId = null
);
