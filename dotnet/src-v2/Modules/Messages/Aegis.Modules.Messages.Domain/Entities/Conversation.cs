using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Modules.Messages.Domain.Entities;

/// <summary>
/// Conversation aggregate root
/// Represents a conversation between two users or a group
/// </summary>
public class Conversation : AggregateRoot<Guid>
{
    private readonly List<Guid> _participantIds = new();

    public string Name { get; private set; }
    public bool IsGroup { get; private set; }
    public Guid? GroupId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public Guid? LastMessageId { get; private set; }

    public IReadOnlyList<Guid> ParticipantIds => _participantIds.AsReadOnly();

    // EF Core constructor
    private Conversation() { }

    private Conversation(
        Guid id,
        string name,
        Guid createdBy,
        IEnumerable<Guid> participantIds,
        bool isGroup = false,
        Guid? groupId = null)
    {
        Id = id;
        Name = name;
        CreatedBy = createdBy;
        IsGroup = isGroup;
        GroupId = groupId;
        CreatedAt = DateTime.UtcNow;
        _participantIds.AddRange(participantIds);
    }

    /// <summary>
    /// Create a new direct conversation (1-on-1)
    /// </summary>
    public static Result<Conversation> CreateDirect(Guid user1Id, Guid user2Id)
    {
        if (user1Id == user2Id)
        {
            return Result.Failure<Conversation>(new Error(
                "Conversation.SameUser",
                "Cannot create conversation with the same user"));
        }

        var participantIds = new[] { user1Id, user2Id };
        var name = $"Direct:{user1Id}:{user2Id}";

        var conversation = new Conversation(
            Guid.NewGuid(),
            name,
            user1Id,
            participantIds,
            isGroup: false);

        return Result.Success(conversation);
    }

    /// <summary>
    /// Create a new group conversation
    /// </summary>
    public static Result<Conversation> CreateGroup(
        Guid groupId,
        string groupName,
        Guid createdBy,
        IEnumerable<Guid> participantIds)
    {
        var participants = participantIds.ToList();

        if (participants.Count < 2)
        {
            return Result.Failure<Conversation>(new Error(
                "Conversation.InsufficientParticipants",
                "Group conversation must have at least 2 participants"));
        }

        if (!participants.Contains(createdBy))
        {
            participants.Add(createdBy);
        }

        var conversation = new Conversation(
            Guid.NewGuid(),
            groupName,
            createdBy,
            participants,
            isGroup: true,
            groupId: groupId);

        return Result.Success(conversation);
    }

    /// <summary>
    /// Update last message information
    /// </summary>
    public void UpdateLastMessage(Guid messageId)
    {
        LastMessageId = messageId;
        LastMessageAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add participant to conversation
    /// </summary>
    public Result AddParticipant(Guid userId)
    {
        if (!IsGroup)
        {
            return Result.Failure(new Error(
                "Conversation.NotGroup",
                "Cannot add participants to direct conversation"));
        }

        if (_participantIds.Contains(userId))
        {
            return Result.Failure(new Error(
                "Conversation.ParticipantExists",
                "User is already a participant"));
        }

        _participantIds.Add(userId);
        return Result.Success();
    }

    /// <summary>
    /// Remove participant from conversation
    /// </summary>
    public Result RemoveParticipant(Guid userId)
    {
        if (!IsGroup)
        {
            return Result.Failure(new Error(
                "Conversation.NotGroup",
                "Cannot remove participants from direct conversation"));
        }

        if (!_participantIds.Contains(userId))
        {
            return Result.Failure(new Error(
                "Conversation.ParticipantNotFound",
                "User is not a participant"));
        }

        _participantIds.Remove(userId);
        return Result.Success();
    }

    /// <summary>
    /// Check if user is participant
    /// </summary>
    public bool IsParticipant(Guid userId)
    {
        return _participantIds.Contains(userId);
    }
}
