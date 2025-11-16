using Aegis.Modules.Groups.Domain.Enums;
using Aegis.Modules.Groups.Domain.ValueObjects;
using Aegis.Shared.Contracts.Events.Groups;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Modules.Groups.Domain.Entities;

public class Group : AggregateRoot<Guid>
{
    private readonly List<GroupMember> _members = new();

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int MaxMembers { get; private set; }

    public IReadOnlyList<GroupMember> Members => _members.AsReadOnly();

    private Group() { }

    private Group(Guid id, string name, string? description, Guid createdBy)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        MaxMembers = 256;

        _members.Add(GroupMember.Create(createdBy, MemberRole.Owner));
    }

    public static Result<Group> Create(string name, string? description, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Group>(new Error("Group.NameRequired", "Group name is required"));

        if (name.Length > 100)
            return Result.Failure<Group>(new Error("Group.NameTooLong", "Group name cannot exceed 100 characters"));

        var group = new Group(Guid.NewGuid(), name, description, createdBy);

        group.RaiseDomainEvent(new GroupCreatedEvent(group.Id, name, createdBy, DateTime.UtcNow));

        return Result.Success(group);
    }

    public Result AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            return Result.Failure(new Error("Group.MemberExists", "User is already a member"));

        if (_members.Count >= MaxMembers)
            return Result.Failure(new Error("Group.MaxMembersReached", "Group has reached maximum members"));

        _members.Add(GroupMember.Create(userId));

        RaiseDomainEvent(new UserJoinedGroupEvent(Id, userId, DateTime.UtcNow));

        return Result.Success();
    }

    public Result RemoveMember(Guid userId, Guid requestingUserId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            return Result.Failure(new Error("Group.MemberNotFound", "User is not a member"));

        if (member.Role == MemberRole.Owner)
            return Result.Failure(new Error("Group.CannotRemoveOwner", "Cannot remove group owner"));

        var requestingMember = _members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requestingMember == null || requestingMember.Role == MemberRole.Member)
            return Result.Failure(new Error("Group.Unauthorized", "Insufficient permissions"));

        _members.Remove(member);
        return Result.Success();
    }

    public Result PromoteToAdmin(Guid userId, Guid requestingUserId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            return Result.Failure(new Error("Group.MemberNotFound", "User is not a member"));

        var requestingMember = _members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requestingMember?.Role != MemberRole.Owner)
            return Result.Failure(new Error("Group.Unauthorized", "Only owner can promote members"));

        member.PromoteToAdmin();
        return Result.Success();
    }

    public Result UpdateInfo(string? name = null, string? description = null, string? avatarUrl = null)
    {
        if (name != null)
        {
            if (name.Length > 100)
                return Result.Failure(new Error("Group.NameTooLong", "Group name cannot exceed 100 characters"));
            Name = name;
        }

        if (description != null)
        {
            if (description.Length > 500)
                return Result.Failure(new Error("Group.DescriptionTooLong", "Description cannot exceed 500 characters"));
            Description = description;
        }

        if (avatarUrl != null)
            AvatarUrl = avatarUrl;

        return Result.Success();
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
}
