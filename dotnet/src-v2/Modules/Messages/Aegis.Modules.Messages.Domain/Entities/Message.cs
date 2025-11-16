using Aegis.Modules.Messages.Domain.Enums;
using Aegis.Modules.Messages.Domain.ValueObjects;
using Aegis.Shared.Contracts.Events.Messages;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Modules.Messages.Domain.Entities;

/// <summary>
/// Message aggregate root
/// Represents an encrypted message between users
/// </summary>
public class Message : AggregateRoot<Guid>
{
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public Guid RecipientId { get; private set; }
    public EncryptedContent Content { get; private set; }
    public MessageType Type { get; private set; }
    public MessageStatus Status { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public Guid? ReplyToMessageId { get; private set; }
    public bool IsGroupMessage { get; private set; }
    public Guid? GroupId { get; private set; }
    public bool IsDeleted { get; private set; }

    // EF Core constructor
    private Message() { }

    private Message(
        Guid id,
        Guid conversationId,
        Guid senderId,
        Guid recipientId,
        EncryptedContent content,
        MessageType type,
        bool isGroupMessage,
        Guid? groupId,
        Guid? replyToMessageId)
    {
        Id = id;
        ConversationId = conversationId;
        SenderId = senderId;
        RecipientId = recipientId;
        Content = content;
        Type = type;
        Status = MessageStatus.Sent;
        SentAt = DateTime.UtcNow;
        IsGroupMessage = isGroupMessage;
        GroupId = groupId;
        ReplyToMessageId = replyToMessageId;
        IsDeleted = false;
    }

    /// <summary>
    /// Create a new message
    /// </summary>
    public static Result<Message> Create(
        Guid conversationId,
        Guid senderId,
        Guid recipientId,
        EncryptedContent content,
        MessageType type = MessageType.Text,
        bool isGroupMessage = false,
        Guid? groupId = null,
        Guid? replyToMessageId = null)
    {
        if (isGroupMessage && groupId == null)
        {
            return Result.Failure<Message>(new Error(
                "Message.GroupIdRequired",
                "Group ID is required for group messages"));
        }

        var message = new Message(
            Guid.NewGuid(),
            conversationId,
            senderId,
            recipientId,
            content,
            type,
            isGroupMessage,
            groupId,
            replyToMessageId);

        // Raise domain event
        message.RaiseDomainEvent(new MessageSentEvent(
            message.Id,
            senderId,
            recipientId,
            isGroupMessage,
            groupId,
            DateTime.UtcNow));

        return Result.Success(message);
    }

    /// <summary>
    /// Mark message as delivered
    /// </summary>
    public Result MarkAsDelivered()
    {
        if (Status == MessageStatus.Read)
        {
            return Result.Failure(new Error(
                "Message.AlreadyRead",
                "Cannot mark as delivered - message already read"));
        }

        if (IsDeleted)
        {
            return Result.Failure(new Error(
                "Message.Deleted",
                "Cannot mark deleted message as delivered"));
        }

        Status = MessageStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;

        // Raise domain event
        RaiseDomainEvent(new MessageDeliveredEvent(
            Id,
            RecipientId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    public Result MarkAsRead()
    {
        if (IsDeleted)
        {
            return Result.Failure(new Error(
                "Message.Deleted",
                "Cannot mark deleted message as read"));
        }

        Status = MessageStatus.Read;
        ReadAt = DateTime.UtcNow;

        if (DeliveredAt == null)
        {
            DeliveredAt = DateTime.UtcNow;
        }

        // Raise domain event
        RaiseDomainEvent(new MessageReadEvent(
            Id,
            RecipientId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark message as failed
    /// </summary>
    public Result MarkAsFailed()
    {
        Status = MessageStatus.Failed;
        return Result.Success();
    }

    /// <summary>
    /// Delete message (soft delete)
    /// </summary>
    public Result Delete()
    {
        IsDeleted = true;
        return Result.Success();
    }
}
