using System;
using System.Collections.Generic;
using Aegis.Core.Models;

namespace Aegis.Data.Entities;

/// <summary>
/// User entity for database
/// </summary>
public class UserEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public string? StatusMessage { get; set; }
    public string? IdentityKey { get; set; }
    public uint RegistrationId { get; set; }
    public uint DeviceId { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public UserPrivacySettings PrivacySettings { get; set; } = new();
}

/// <summary>
/// Message entity for database
/// </summary>
public class MessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public byte[] EncryptedContent { get; set; } = Array.Empty<byte>();
    public MessageType Type { get; set; } = MessageType.Regular;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsGroup { get; set; }
    public Guid? GroupId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public bool IsSealedSender { get; set; }
    public Guid? FileAttachmentId { get; set; }
    public long? ServerMessageId { get; set; }
}

/// <summary>
/// Group entity for database
/// </summary>
public class GroupEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Avatar { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<GroupMemberEntity> Members { get; set; } = new();
    public int MaxMembers { get; set; } = 256;
    public GroupType Type { get; set; } = GroupType.Private;
    public string? SenderKeyId { get; set; }
    public DateTime? SenderKeyRotatedAt { get; set; }
    public GroupSettings Settings { get; set; } = new();
}

/// <summary>
/// Group member entity
/// </summary>
public class GroupMemberEntity
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public GroupRole Role { get; set; } = GroupRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string? Nickname { get; set; }
    public bool IsMuted { get; set; }
}

/// <summary>
/// Contact entity for database
/// </summary>
public class ContactEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public Guid ContactUserId { get; set; }
    public string? Nickname { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public bool IsBlocked { get; set; }
    public bool IsFavorite { get; set; }
    public SafetyNumberStatus SafetyNumberStatus { get; set; } = SafetyNumberStatus.Unverified;
    public string? SafetyNumber { get; set; }
    public DateTime? SafetyNumberVerifiedAt { get; set; }
}

/// <summary>
/// Pre-key bundle entity for database
/// </summary>
public class PreKeyBundleEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public uint RegistrationId { get; set; }
    public uint DeviceId { get; set; } = 1;
    public uint PreKeyId { get; set; }
    public string PreKeyPublic { get; set; } = string.Empty;
    public uint SignedPreKeyId { get; set; }
    public string SignedPreKeyPublic { get; set; } = string.Empty;
    public string SignedPreKeySignature { get; set; } = string.Empty;
    public string IdentityKey { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; }
}

/// <summary>
/// File attachment entity for database
/// </summary>
public class FileAttachmentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = "application/octet-stream";
    public byte[]? EncryptedContent { get; set; }
    public string? FileUrl { get; set; }
    public string? EncryptionKey { get; set; }
    public string? IV { get; set; }
    public string? FileHash { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid UploaderId { get; set; }
    public bool IsImage { get; set; }
    public string? Thumbnail { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
