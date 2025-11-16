using Aegis.Modules.Users.Domain.Enums;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;
using Aegis.Shared.Kernel.ValueObjects;

namespace Aegis.Modules.Users.Domain.Entities;

/// <summary>
/// User profile aggregate root
/// Extended user information beyond authentication
/// </summary>
public class UserProfile : AggregateRoot<Guid>
{
    private readonly List<Guid> _contactIds = new();
    private readonly List<Guid> _blockedUserIds = new();

    public string? DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public OnlineStatus Status { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public bool ShowOnlineStatus { get; private set; }

    // Privacy settings
    public PrivacySettings PrivacySettings { get; private set; }

    public IReadOnlyList<Guid> ContactIds => _contactIds.AsReadOnly();
    public IReadOnlyList<Guid> BlockedUserIds => _blockedUserIds.AsReadOnly();

    // EF Core constructor
    private UserProfile() { }

    private UserProfile(Guid userId)
    {
        Id = userId;
        Status = OnlineStatus.Offline;
        ShowOnlineStatus = true;
        PrivacySettings = PrivacySettings.CreateDefault();  // Balanced privacy by default
    }

    /// <summary>
    /// Create user profile
    /// </summary>
    public static Result<UserProfile> Create(Guid userId)
    {
        var profile = new UserProfile(userId);
        return Result.Success(profile);
    }

    /// <summary>
    /// Update profile information
    /// </summary>
    public Result UpdateProfile(string? displayName = null, string? bio = null, string? avatarUrl = null)
    {
        if (displayName != null)
        {
            if (displayName.Length > 100)
            {
                return Result.Failure(new Error(
                    "UserProfile.DisplayNameTooLong",
                    "Display name cannot exceed 100 characters"));
            }
            DisplayName = displayName;
        }

        if (bio != null)
        {
            if (bio.Length > 500)
            {
                return Result.Failure(new Error(
                    "UserProfile.BioTooLong",
                    "Bio cannot exceed 500 characters"));
            }
            Bio = bio;
        }

        if (avatarUrl != null)
        {
            AvatarUrl = avatarUrl;
        }

        return Result.Success();
    }

    /// <summary>
    /// Set online status
    /// </summary>
    public Result SetStatus(OnlineStatus status)
    {
        Status = status;

        if (status == OnlineStatus.Offline)
        {
            LastSeenAt = DateTime.UtcNow;
        }

        return Result.Success();
    }

    /// <summary>
    /// Toggle online status visibility
    /// </summary>
    public void SetOnlineStatusVisibility(bool show)
    {
        ShowOnlineStatus = show;
    }

    /// <summary>
    /// Add contact
    /// </summary>
    public Result AddContact(Guid userId)
    {
        if (_contactIds.Contains(userId))
        {
            return Result.Failure(new Error(
                "UserProfile.ContactExists",
                "User is already in contacts"));
        }

        if (_blockedUserIds.Contains(userId))
        {
            return Result.Failure(new Error(
                "UserProfile.UserBlocked",
                "Cannot add blocked user to contacts"));
        }

        _contactIds.Add(userId);
        return Result.Success();
    }

    /// <summary>
    /// Remove contact
    /// </summary>
    public Result RemoveContact(Guid userId)
    {
        if (!_contactIds.Contains(userId))
        {
            return Result.Failure(new Error(
                "UserProfile.ContactNotFound",
                "User is not in contacts"));
        }

        _contactIds.Remove(userId);
        return Result.Success();
    }

    /// <summary>
    /// Block user
    /// </summary>
    public Result BlockUser(Guid userId)
    {
        if (_blockedUserIds.Contains(userId))
        {
            return Result.Failure(new Error(
                "UserProfile.AlreadyBlocked",
                "User is already blocked"));
        }

        _blockedUserIds.Add(userId);

        // Remove from contacts if present
        _contactIds.Remove(userId);

        return Result.Success();
    }

    /// <summary>
    /// Unblock user
    /// </summary>
    public Result UnblockUser(Guid userId)
    {
        if (!_blockedUserIds.Contains(userId))
        {
            return Result.Failure(new Error(
                "UserProfile.NotBlocked",
                "User is not blocked"));
        }

        _blockedUserIds.Remove(userId);
        return Result.Success();
    }

    /// <summary>
    /// Check if user is blocked
    /// </summary>
    public bool IsBlocked(Guid userId) => _blockedUserIds.Contains(userId);

    /// <summary>
    /// Check if user is contact
    /// </summary>
    public bool IsContact(Guid userId) => _contactIds.Contains(userId);

    /// <summary>
    /// Update privacy settings
    /// </summary>
    public Result UpdatePrivacySettings(PrivacySettings newSettings)
    {
        PrivacySettings = newSettings;
        return Result.Success();
    }

    /// <summary>
    /// Check if requester can see online status based on privacy settings
    /// </summary>
    public bool CanSeeOnlineStatus(Guid requesterId)
    {
        var isContact = _contactIds.Contains(requesterId);
        return PrivacySettings.CanSee(PrivacySettings.OnlineStatusVisibility, isContact);
    }

    /// <summary>
    /// Check if requester can see last seen based on privacy settings
    /// </summary>
    public bool CanSeeLastSeen(Guid requesterId)
    {
        var isContact = _contactIds.Contains(requesterId);
        return PrivacySettings.CanSee(PrivacySettings.LastSeenVisibility, isContact);
    }

    /// <summary>
    /// Check if requester can see profile picture based on privacy settings
    /// </summary>
    public bool CanSeeProfilePicture(Guid requesterId)
    {
        var isContact = _contactIds.Contains(requesterId);
        return PrivacySettings.CanSee(PrivacySettings.ProfilePictureVisibility, isContact);
    }

    /// <summary>
    /// Check if requester can see bio based on privacy settings
    /// </summary>
    public bool CanSeeBio(Guid requesterId)
    {
        var isContact = _contactIds.Contains(requesterId);
        return PrivacySettings.CanSee(PrivacySettings.BioVisibility, isContact);
    }
}
