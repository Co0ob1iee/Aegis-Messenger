using System;
using System.Collections.Generic;

namespace Aegis.Core.Models;

/// <summary>
/// Represents a group chat in the Aegis Messenger system
/// Groups use Signal Protocol's Sender Key for efficient encryption
/// </summary>
public class Group
{
    /// <summary>
    /// Unique identifier for the group
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Group name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Group description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Group avatar/picture URL or base64 encoded
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// ID of the user who created the group
    /// </summary>
    public Guid CreatorId { get; set; }

    /// <summary>
    /// Group creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Group members
    /// </summary>
    public List<GroupMember> Members { get; set; } = new();

    /// <summary>
    /// Maximum number of members allowed
    /// </summary>
    public int MaxMembers { get; set; } = 256;

    /// <summary>
    /// Group type (private, public, broadcast)
    /// </summary>
    public GroupType Type { get; set; } = GroupType.Private;

    /// <summary>
    /// Sender key ID for group encryption (rotated periodically)
    /// </summary>
    public string? SenderKeyId { get; set; }

    /// <summary>
    /// Last time sender key was rotated
    /// </summary>
    public DateTime? SenderKeyRotatedAt { get; set; }

    /// <summary>
    /// Group settings
    /// </summary>
    public GroupSettings Settings { get; set; } = new();
}

/// <summary>
/// Represents a member of a group
/// </summary>
public class GroupMember
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User role in the group
    /// </summary>
    public GroupRole Role { get; set; } = GroupRole.Member;

    /// <summary>
    /// When the user joined the group
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Custom nickname in this group (optional)
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// Flag indicating if user is muted in this group
    /// </summary>
    public bool IsMuted { get; set; }
}

/// <summary>
/// Group type enumeration
/// </summary>
public enum GroupType
{
    /// <summary>
    /// Private group (invite-only)
    /// </summary>
    Private = 1,

    /// <summary>
    /// Public group (anyone can join with link)
    /// </summary>
    Public = 2,

    /// <summary>
    /// Broadcast group (only admins can send messages)
    /// </summary>
    Broadcast = 3
}

/// <summary>
/// Member role in a group
/// </summary>
public enum GroupRole
{
    /// <summary>
    /// Regular member
    /// </summary>
    Member = 1,

    /// <summary>
    /// Group administrator
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Group owner (creator)
    /// </summary>
    Owner = 3
}

/// <summary>
/// Group settings
/// </summary>
public class GroupSettings
{
    /// <summary>
    /// Only admins can send messages
    /// </summary>
    public bool OnlyAdminsCanMessage { get; set; }

    /// <summary>
    /// Only admins can change group info
    /// </summary>
    public bool OnlyAdminsCanEdit { get; set; } = true;

    /// <summary>
    /// Require admin approval for new members
    /// </summary>
    public bool RequireAdminApproval { get; set; }

    /// <summary>
    /// Allow members to add others
    /// </summary>
    public bool MembersCanAddOthers { get; set; } = true;

    /// <summary>
    /// Disappearing messages enabled
    /// </summary>
    public bool DisappearingMessagesEnabled { get; set; }

    /// <summary>
    /// Disappearing messages timer (in seconds)
    /// </summary>
    public int DisappearingMessagesTimer { get; set; } = 86400; // 24 hours default
}
