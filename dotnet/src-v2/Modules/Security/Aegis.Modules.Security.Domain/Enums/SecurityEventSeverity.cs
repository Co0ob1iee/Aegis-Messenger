namespace Aegis.Modules.Security.Domain.Enums;

/// <summary>
/// Severity level of security event
/// </summary>
public enum SecurityEventSeverity
{
    /// <summary>
    /// Informational event - normal operation
    /// </summary>
    Info = 0,

    /// <summary>
    /// Low severity - minor issues
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity - should be reviewed
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity - requires attention
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical severity - immediate action required
    /// </summary>
    Critical = 4
}
