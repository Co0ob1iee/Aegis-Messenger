using System;

namespace Aegis.Core.Models;

/// <summary>
/// Represents a user in the Aegis Messenger system
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Username (unique)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address (optional, for recovery)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Phone number (optional, for contact discovery)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Display name shown to contacts
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Profile picture URL or base64 encoded image
    /// </summary>
    public string? ProfilePicture { get; set; }

    /// <summary>
    /// User status message
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Public identity key (long-term Signal Protocol key)
    /// Base64 encoded
    /// </summary>
    public string? IdentityKey { get; set; }

    /// <summary>
    /// Registration ID (Signal Protocol)
    /// </summary>
    public uint RegistrationId { get; set; }

    /// <summary>
    /// Device ID (for multi-device support)
    /// </summary>
    public uint DeviceId { get; set; } = 1;

    /// <summary>
    /// Account created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last seen timestamp
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>
    /// Flag indicating if user is currently online
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Hashed password (using bcrypt or Argon2)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt for password hashing
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// Flag for two-factor authentication enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// TOTP secret for 2FA
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// Account status (active, suspended, deleted)
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// Privacy settings
    /// </summary>
    public UserPrivacySettings PrivacySettings { get; set; } = new();
}

/// <summary>
/// User account status
/// </summary>
public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    Deleted = 3,
    Banned = 4
}

/// <summary>
/// User privacy settings
/// </summary>
public class UserPrivacySettings
{
    /// <summary>
    /// Who can see profile picture
    /// </summary>
    public PrivacyLevel ProfilePictureVisibility { get; set; } = PrivacyLevel.Everyone;

    /// <summary>
    /// Who can see status message
    /// </summary>
    public PrivacyLevel StatusVisibility { get; set; } = PrivacyLevel.Everyone;

    /// <summary>
    /// Who can see last seen timestamp
    /// </summary>
    public PrivacyLevel LastSeenVisibility { get; set; } = PrivacyLevel.Contacts;

    /// <summary>
    /// Enable read receipts
    /// </summary>
    public bool ReadReceipts { get; set; } = true;

    /// <summary>
    /// Enable typing indicators
    /// </summary>
    public bool TypingIndicators { get; set; } = true;
}

/// <summary>
/// Privacy level enumeration
/// </summary>
public enum PrivacyLevel
{
    Everyone = 1,
    Contacts = 2,
    Nobody = 3
}
