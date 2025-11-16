using Aegis.Modules.Security.Domain.Enums;

namespace Aegis.Modules.Security.Application.Alerting;

/// <summary>
/// Represents a security alert to be sent via email or webhook
/// </summary>
public sealed class SecurityAlert
{
    public Guid EventId { get; init; }
    public SecurityEventType EventType { get; init; }
    public SecurityEventSeverity Severity { get; init; }
    public DateTime Timestamp { get; init; }
    public Guid? UserId { get; init; }
    public string? Username { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsSuccessful { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Details { get; init; }

    public static SecurityAlert FromAuditLog(Domain.Entities.SecurityAuditLog auditLog, string? username = null)
    {
        return new SecurityAlert
        {
            EventId = auditLog.Id,
            EventType = auditLog.EventType,
            Severity = auditLog.Severity,
            Timestamp = auditLog.Timestamp,
            UserId = auditLog.UserId,
            Username = username,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            IsSuccessful = auditLog.IsSuccessful,
            ErrorMessage = auditLog.ErrorMessage,
            Details = auditLog.Details
        };
    }

    public string GetTitle()
    {
        var status = IsSuccessful ? "Success" : "Failed";
        var severityEmoji = Severity switch
        {
            SecurityEventSeverity.Critical => "🚨",
            SecurityEventSeverity.High => "⚠️",
            SecurityEventSeverity.Medium => "⚡",
            SecurityEventSeverity.Low => "ℹ️",
            _ => "📋"
        };

        return $"{severityEmoji} [{Severity}] {EventType} - {status}";
    }

    public string GetDescription()
    {
        var lines = new List<string>
        {
            $"**Event Type:** {EventType}",
            $"**Severity:** {Severity}",
            $"**Status:** {(IsSuccessful ? "✅ Success" : "❌ Failed")}",
            $"**Timestamp:** {Timestamp:yyyy-MM-dd HH:mm:ss} UTC"
        };

        if (UserId.HasValue)
            lines.Add($"**User ID:** {UserId.Value}");

        if (!string.IsNullOrEmpty(Username))
            lines.Add($"**Username:** {Username}");

        if (!string.IsNullOrEmpty(IpAddress))
            lines.Add($"**IP Address:** {IpAddress}");

        if (!string.IsNullOrEmpty(ErrorMessage))
            lines.Add($"**Error:** {ErrorMessage}");

        if (!string.IsNullOrEmpty(Details))
            lines.Add($"**Details:** {Details}");

        return string.Join("\n", lines);
    }
}
