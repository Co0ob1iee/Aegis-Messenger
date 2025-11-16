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

    // Disappearing messages
    public TimeSpan? DisappearDuration { get; private set; }
    public DateTime? DisappearsAt { get; private set; }
    public bool IsExpired => DisappearsAt.HasValue && DateTime.UtcNow >= DisappearsAt.Value;

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
        Guid? replyToMessageId = null,
        TimeSpan? disappearDuration = null)
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

        // Set disappearing message timer if specified
        if (disappearDuration.HasValue)
        {
            var setResult = message.SetDisappearing(disappearDuration.Value);
            if (setResult.IsFailure)
                return Result.Failure<Message>(setResult.Error);
        }

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

        // Start disappearing message timer if configured
        UpdateDisappearingTimer();

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

    /// <summary>
    /// Set disappearing message timer
    /// Message will be deleted after specified duration from when it's read
    /// </summary>
    public Result SetDisappearing(TimeSpan duration)
    {
        // Supported durations
        var supportedDurations = new[]
        {
            TimeSpan.FromSeconds(30),   // 30 seconds
            TimeSpan.FromMinutes(1),    // 1 minute
            TimeSpan.FromMinutes(5),    // 5 minutes
            TimeSpan.FromMinutes(30),   // 30 minutes
            TimeSpan.FromHours(1),      // 1 hour
            TimeSpan.FromHours(8),      // 8 hours
            TimeSpan.FromDays(1),       // 24 hours
            TimeSpan.FromDays(7),       // 7 days
        };

        if (!supportedDurations.Contains(duration))
        {
            return Result.Failure(new Error(
                "Message.InvalidDisappearDuration",
                $"Duration must be one of: {string.Join(", ", supportedDurations.Select(d => d.TotalSeconds + "s"))}"));
        }

        DisappearDuration = duration;

        // Calculate expiration time
        // If message is already read, start timer now
        // Otherwise, timer starts when message is read
        if (ReadAt.HasValue)
        {
            DisappearsAt = ReadAt.Value + duration;
        }

        return Result.Success();
    }

    /// <summary>
    /// Clear disappearing message timer
    /// </summary>
    public Result ClearDisappearing()
    {
        DisappearDuration = null;
        DisappearsAt = null;
        return Result.Success();
    }

    /// <summary>
    /// Update expiration time when message is read
    /// (for disappearing messages)
    /// </summary>
    private void UpdateDisappearingTimer()
    {
        if (DisappearDuration.HasValue && ReadAt.HasValue && !DisappearsAt.HasValue)
        {
            DisappearsAt = ReadAt.Value + DisappearDuration.Value;
        }
    }
}
