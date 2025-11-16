namespace Aegis.Modules.Messages.Domain.Enums;

/// <summary>
/// Type of message content
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Text message
    /// </summary>
    Text = 0,

    /// <summary>
    /// Image attachment
    /// </summary>
    Image = 1,

    /// <summary>
    /// File attachment
    /// </summary>
    File = 2,

    /// <summary>
    /// Voice message
    /// </summary>
    Voice = 3,

    /// <summary>
    /// Video attachment
    /// </summary>
    Video = 4,

    /// <summary>
    /// System message (user joined, left, etc.)
    /// </summary>
    System = 5
}
