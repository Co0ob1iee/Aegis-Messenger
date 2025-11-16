using Aegis.Modules.Groups.Domain.Enums;
using Aegis.Shared.Kernel.Primitives;

namespace Aegis.Modules.Groups.Domain.ValueObjects;

public sealed class GroupMember : ValueObject
{
    public Guid UserId { get; }
    public MemberRole Role { get; private set; }
    public DateTime JoinedAt { get; }

    private GroupMember(Guid userId, MemberRole role)
    {
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    public static GroupMember Create(Guid userId, MemberRole role = MemberRole.Member)
    {
        return new GroupMember(userId, role);
    }

    public void PromoteToAdmin() => Role = MemberRole.Admin;
    public void DemoteToMember() => Role = MemberRole.Member;

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return UserId;
        yield return Role;
    }
}
