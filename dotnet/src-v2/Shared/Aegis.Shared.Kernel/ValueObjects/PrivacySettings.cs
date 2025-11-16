using Aegis.Shared.Kernel.Enums;
using Aegis.Shared.Kernel.Primitives;

namespace Aegis.Shared.Kernel.ValueObjects;

/// <summary>
/// Privacy settings value object
/// Controls user's privacy and metadata visibility
/// </summary>
public sealed class PrivacySettings : ValueObject
{
    /// <summary>
    /// Who can see online status
    /// </summary>
    public PrivacyLevel OnlineStatusVisibility { get; private set; }

    /// <summary>
    /// Who can see last seen timestamp
    /// </summary>
    public PrivacyLevel LastSeenVisibility { get; private set; }

    /// <summary>
    /// Who can see profile picture
    /// </summary>
    public PrivacyLevel ProfilePictureVisibility { get; private set; }

    /// <summary>
    /// Who can see bio/about
    /// </summary>
    public PrivacyLevel BioVisibility { get; private set; }

    /// <summary>
    /// Send read receipts (double check marks)
    /// </summary>
    public bool SendReadReceipts { get; private set; }

    /// <summary>
    /// Send delivery receipts (single check mark)
    /// </summary>
    public bool SendDeliveryReceipts { get; private set; }

    /// <summary>
    /// Send typing indicators ("user is typing...")
    /// </summary>
    public bool SendTypingIndicators { get; private set; }

    /// <summary>
    /// Default message expiration time (disappearing messages)
    /// Null = messages don't disappear by default
    /// </summary>
    public TimeSpan? DefaultMessageExpiration { get; private set; }

    /// <summary>
    /// Fuzz timestamps to nearest minute for privacy
    /// Prevents timing correlation attacks
    /// </summary>
    public bool FuzzTimestamps { get; private set; }

    /// <summary>
    /// Use message padding to hide message length
    /// Prevents traffic analysis
    /// </summary>
    public bool UseMessagePadding { get; private set; }

    private PrivacySettings() { }

    private PrivacySettings(
        PrivacyLevel onlineStatusVisibility,
        PrivacyLevel lastSeenVisibility,
        PrivacyLevel profilePictureVisibility,
        PrivacyLevel bioVisibility,
        bool sendReadReceipts,
        bool sendDeliveryReceipts,
        bool sendTypingIndicators,
        TimeSpan? defaultMessageExpiration,
        bool fuzzTimestamps,
        bool useMessagePadding)
    {
        OnlineStatusVisibility = onlineStatusVisibility;
        LastSeenVisibility = lastSeenVisibility;
        ProfilePictureVisibility = profilePictureVisibility;
        BioVisibility = bioVisibility;
        SendReadReceipts = sendReadReceipts;
        SendDeliveryReceipts = sendDeliveryReceipts;
        SendTypingIndicators = sendTypingIndicators;
        DefaultMessageExpiration = defaultMessageExpiration;
        FuzzTimestamps = fuzzTimestamps;
        UseMessagePadding = useMessagePadding;
    }

    /// <summary>
    /// Create default privacy settings (balanced privacy)
    /// </summary>
    public static PrivacySettings CreateDefault()
    {
        return new PrivacySettings(
            onlineStatusVisibility: PrivacyLevel.Contacts,
            lastSeenVisibility: PrivacyLevel.Contacts,
            profilePictureVisibility: PrivacyLevel.Contacts,
            bioVisibility: PrivacyLevel.Contacts,
            sendReadReceipts: true,
            sendDeliveryReceipts: true,
            sendTypingIndicators: true,
            defaultMessageExpiration: null,
            fuzzTimestamps: true,
            useMessagePadding: true);
    }

    /// <summary>
    /// Create maximum privacy settings
    /// </summary>
    public static PrivacySettings CreateMaximumPrivacy()
    {
        return new PrivacySettings(
            onlineStatusVisibility: PrivacyLevel.Nobody,
            lastSeenVisibility: PrivacyLevel.Nobody,
            profilePictureVisibility: PrivacyLevel.Contacts,
            bioVisibility: PrivacyLevel.Contacts,
            sendReadReceipts: false,
            sendDeliveryReceipts: false,
            sendTypingIndicators: false,
            defaultMessageExpiration: TimeSpan.FromDays(7),  // 7 days
            fuzzTimestamps: true,
            useMessagePadding: true);
    }

    /// <summary>
    /// Create minimum privacy settings (maximum convenience)
    /// </summary>
    public static PrivacySettings CreateMinimumPrivacy()
    {
        return new PrivacySettings(
            onlineStatusVisibility: PrivacyLevel.Everyone,
            lastSeenVisibility: PrivacyLevel.Everyone,
            profilePictureVisibility: PrivacyLevel.Everyone,
            bioVisibility: PrivacyLevel.Everyone,
            sendReadReceipts: true,
            sendDeliveryReceipts: true,
            sendTypingIndicators: true,
            defaultMessageExpiration: null,
            fuzzTimestamps: false,
            useMessagePadding: false);
    }

    /// <summary>
    /// Update online status visibility
    /// </summary>
    public PrivacySettings WithOnlineStatusVisibility(PrivacyLevel level)
    {
        return new PrivacySettings(
            level,
            LastSeenVisibility,
            ProfilePictureVisibility,
            BioVisibility,
            SendReadReceipts,
            SendDeliveryReceipts,
            SendTypingIndicators,
            DefaultMessageExpiration,
            FuzzTimestamps,
            UseMessagePadding);
    }

    /// <summary>
    /// Update last seen visibility
    /// </summary>
    public PrivacySettings WithLastSeenVisibility(PrivacyLevel level)
    {
        return new PrivacySettings(
            OnlineStatusVisibility,
            level,
            ProfilePictureVisibility,
            BioVisibility,
            SendReadReceipts,
            SendDeliveryReceipts,
            SendTypingIndicators,
            DefaultMessageExpiration,
            FuzzTimestamps,
            UseMessagePadding);
    }

    /// <summary>
    /// Update read receipts setting
    /// </summary>
    public PrivacySettings WithReadReceipts(bool enabled)
    {
        return new PrivacySettings(
            OnlineStatusVisibility,
            LastSeenVisibility,
            ProfilePictureVisibility,
            BioVisibility,
            enabled,
            SendDeliveryReceipts,
            SendTypingIndicators,
            DefaultMessageExpiration,
            FuzzTimestamps,
            UseMessagePadding);
    }

    /// <summary>
    /// Update default message expiration
    /// </summary>
    public PrivacySettings WithDefaultMessageExpiration(TimeSpan? expiration)
    {
        return new PrivacySettings(
            OnlineStatusVisibility,
            LastSeenVisibility,
            ProfilePictureVisibility,
            BioVisibility,
            SendReadReceipts,
            SendDeliveryReceipts,
            SendTypingIndicators,
            expiration,
            FuzzTimestamps,
            UseMessagePadding);
    }

    /// <summary>
    /// Update timestamp fuzzing
    /// </summary>
    public PrivacySettings WithTimestampFuzzing(bool enabled)
    {
        return new PrivacySettings(
            OnlineStatusVisibility,
            LastSeenVisibility,
            ProfilePictureVisibility,
            BioVisibility,
            SendReadReceipts,
            SendDeliveryReceipts,
            SendTypingIndicators,
            DefaultMessageExpiration,
            enabled,
            UseMessagePadding);
    }

    /// <summary>
    /// Update message padding
    /// </summary>
    public PrivacySettings WithMessagePadding(bool enabled)
    {
        return new PrivacySettings(
            OnlineStatusVisibility,
            LastSeenVisibility,
            ProfilePictureVisibility,
            BioVisibility,
            SendReadReceipts,
            SendDeliveryReceipts,
            SendTypingIndicators,
            DefaultMessageExpiration,
            FuzzTimestamps,
            enabled);
    }

    /// <summary>
    /// Check if user can see information based on privacy level and relationship
    /// </summary>
    public bool CanSee(PrivacyLevel setting, bool isContact)
    {
        return setting switch
        {
            PrivacyLevel.Everyone => true,
            PrivacyLevel.Contacts => isContact,
            PrivacyLevel.Nobody => false,
            _ => false
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return OnlineStatusVisibility;
        yield return LastSeenVisibility;
        yield return ProfilePictureVisibility;
        yield return BioVisibility;
        yield return SendReadReceipts;
        yield return SendDeliveryReceipts;
        yield return SendTypingIndicators;
        yield return DefaultMessageExpiration;
        yield return FuzzTimestamps;
        yield return UseMessagePadding;
    }
}
