namespace Aegis.Modules.Security.Domain.Enums;

/// <summary>
/// Types of security events to audit
/// </summary>
public enum SecurityEventType
{
    // Authentication Events
    LoginSuccess = 100,
    LoginFailed = 101,
    Logout = 102,

    // Account Management
    AccountCreated = 200,
    AccountDeleted = 201,
    PasswordChanged = 202,
    EmailChanged = 203,

    // Cryptography Events
    KeyGenerated = 300,
    KeyRotated = 301,
    KeyVerified = 302,
    SessionInitialized = 303,
    SessionDeleted = 304,

    // Privacy Events
    PrivacySettingsChanged = 400,
    DisappearingMessagesEnabled = 401,
    DisappearingMessagesDisabled = 402,

    // User Actions
    UserBlocked = 500,
    UserUnblocked = 501,
    ContactAdded = 502,
    ContactRemoved = 503,

    // Message Events
    MessageSent = 600,
    MessageDeleted = 601,
    MessageExpired = 602,

    // Group Events
    GroupCreated = 700,
    GroupDeleted = 701,
    UserJoinedGroup = 702,
    UserLeftGroup = 703,
    UserPromoted = 704,
    UserDemoted = 705,

    // File Events
    FileUploaded = 800,
    FileDownloaded = 801,
    FileDeleted = 802,

    // Security Events
    RateLimitExceeded = 900,
    SuspiciousActivity = 901,
    InvalidToken = 902,
    UnauthorizedAccess = 903,

    // System Events
    ServiceStarted = 1000,
    ServiceStopped = 1001,
    BackgroundJobExecuted = 1002
}
