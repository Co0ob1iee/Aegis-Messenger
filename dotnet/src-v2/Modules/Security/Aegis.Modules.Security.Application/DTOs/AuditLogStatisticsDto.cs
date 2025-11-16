using Aegis.Modules.Security.Domain.Enums;

namespace Aegis.Modules.Security.Application.DTOs;

/// <summary>
/// Statistics for audit logs dashboard
/// </summary>
public sealed class AuditLogStatisticsDto
{
    public int TotalEvents { get; init; }
    public int TotalFailures { get; init; }
    public int CriticalEvents { get; init; }
    public int HighSeverityEvents { get; init; }

    public Dictionary<SecurityEventType, int> EventTypeCounts { get; init; } = new();
    public Dictionary<SecurityEventSeverity, int> SeverityCounts { get; init; } = new();

    public List<TopUserActivity> TopActiveUsers { get; init; } = new();
    public List<TopIpActivity> TopIpAddresses { get; init; } = new();

    public DateTime? OldestEvent { get; init; }
    public DateTime? NewestEvent { get; init; }

    public sealed class TopUserActivity
    {
        public Guid UserId { get; init; }
        public string? Username { get; init; }
        public int EventCount { get; init; }
        public int FailureCount { get; init; }
    }

    public sealed class TopIpActivity
    {
        public string IpAddress { get; init; } = string.Empty;
        public int EventCount { get; init; }
        public int FailureCount { get; init; }
    }
}
