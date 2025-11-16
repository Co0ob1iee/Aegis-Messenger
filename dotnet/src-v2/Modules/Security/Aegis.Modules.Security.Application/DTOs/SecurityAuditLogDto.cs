using Aegis.Modules.Security.Domain.Enums;

namespace Aegis.Modules.Security.Application.DTOs;

/// <summary>
/// DTO for security audit log entry
/// </summary>
public sealed class SecurityAuditLogDto
{
    public Guid Id { get; init; }
    public SecurityEventType EventType { get; init; }
    public SecurityEventSeverity Severity { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsSuccessful { get; init; }

    public Guid? UserId { get; init; }
    public string? Username { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }

    public string? ErrorMessage { get; init; }
    public string? Details { get; init; }

    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }

    public string? RequestPath { get; init; }
    public string? RequestMethod { get; init; }
    public int? ResponseStatusCode { get; init; }
    public TimeSpan? RequestDuration { get; init; }
}
