namespace Aegis.Shared.Kernel.Enums;

/// <summary>
/// Privacy level for user settings
/// Controls who can see specific information
/// </summary>
public enum PrivacyLevel
{
    /// <summary>
    /// Everyone can see
    /// </summary>
    Everyone = 0,

    /// <summary>
    /// Only contacts can see
    /// </summary>
    Contacts = 1,

    /// <summary>
    /// Nobody can see (maximum privacy)
    /// </summary>
    Nobody = 2
}
