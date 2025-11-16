using Aegis.Modules.Security.Domain.Enums;
using Aegis.Shared.Kernel.Primitives;

namespace Aegis.Modules.Security.Domain.Entities;

/// <summary>
/// Security audit log entry
/// Tracks all security-relevant events in the system
/// </summary>
public class SecurityAuditLog : Entity<Guid>
{
    public Guid? UserId { get; private set; }
    public SecurityEventType EventType { get; private set; }
    public SecurityEventSeverity Severity { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Details { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }

    // EF Core constructor
    private SecurityAuditLog() { }

    private SecurityAuditLog(
        Guid id,
        Guid? userId,
        SecurityEventType eventType,
        SecurityEventSeverity severity,
        bool isSuccessful,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        string? errorMessage = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        Id = id;
        UserId = userId;
        EventType = eventType;
        Severity = severity;
        IsSuccessful = isSuccessful;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Details = details;
        ErrorMessage = errorMessage;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Create successful security event
    /// </summary>
    public static SecurityAuditLog CreateSuccess(
        SecurityEventType eventType,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        var severity = DetermineSeverity(eventType, isSuccess: true);

        return new SecurityAuditLog(
            Guid.NewGuid(),
            userId,
            eventType,
            severity,
            isSuccessful: true,
            ipAddress,
            userAgent,
            details,
            errorMessage: null,
            relatedEntityId,
            relatedEntityType);
    }

    /// <summary>
    /// Create failed security event
    /// </summary>
    public static SecurityAuditLog CreateFailure(
        SecurityEventType eventType,
        string errorMessage,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        var severity = DetermineSeverity(eventType, isSuccess: false);

        return new SecurityAuditLog(
            Guid.NewGuid(),
            userId,
            eventType,
            severity,
            isSuccessful: false,
            ipAddress,
            userAgent,
            details,
            errorMessage,
            relatedEntityId,
            relatedEntityType);
    }

    /// <summary>
    /// Determine severity based on event type and success status
    /// </summary>
    private static SecurityEventSeverity DetermineSeverity(SecurityEventType eventType, bool isSuccess)
    {
        // Failed security events are generally more severe
        if (!isSuccess)
        {
            return eventType switch
            {
                SecurityEventType.LoginFailed => SecurityEventSeverity.Medium,
                SecurityEventType.InvalidToken => SecurityEventSeverity.High,
                SecurityEventType.UnauthorizedAccess => SecurityEventSeverity.High,
                SecurityEventType.RateLimitExceeded => SecurityEventSeverity.High,
                SecurityEventType.SuspiciousActivity => SecurityEventSeverity.Critical,
                _ => SecurityEventSeverity.Medium
            };
        }

        // Successful events
        return eventType switch
        {
            // Critical operations
            SecurityEventType.AccountDeleted => SecurityEventSeverity.High,
            SecurityEventType.PasswordChanged => SecurityEventSeverity.Medium,
            SecurityEventType.KeyRotated => SecurityEventSeverity.Medium,
            SecurityEventType.PrivacySettingsChanged => SecurityEventSeverity.Low,

            // Security events
            SecurityEventType.RateLimitExceeded => SecurityEventSeverity.High,
            SecurityEventType.SuspiciousActivity => SecurityEventSeverity.Critical,
            SecurityEventType.UnauthorizedAccess => SecurityEventSeverity.Critical,

            // Normal operations
            SecurityEventType.LoginSuccess => SecurityEventSeverity.Info,
            SecurityEventType.MessageSent => SecurityEventSeverity.Info,

            _ => SecurityEventSeverity.Info
        };
    }

    /// <summary>
    /// Check if this event should trigger an alert
    /// </summary>
    public bool ShouldAlert()
    {
        return Severity >= SecurityEventSeverity.High ||
               (!IsSuccessful && Severity >= SecurityEventSeverity.Medium);
    }
}
