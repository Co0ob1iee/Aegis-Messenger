using Aegis.Modules.Security.Domain.Entities;
using Aegis.Modules.Security.Domain.Enums;

namespace Aegis.Modules.Security.Domain.Repositories;

/// <summary>
/// Repository for security audit logs
/// </summary>
public interface ISecurityAuditRepository
{
    /// <summary>
    /// Add new audit log entry
    /// </summary>
    Task<SecurityAuditLog> AddAsync(SecurityAuditLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a user
    /// </summary>
    Task<IReadOnlyList<SecurityAuditLog>> GetUserLogsAsync(
        Guid userId,
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by event type
    /// </summary>
    Task<IReadOnlyList<SecurityAuditLog>> GetByEventTypeAsync(
        SecurityEventType eventType,
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed events
    /// </summary>
    Task<IReadOnlyList<SecurityAuditLog>> GetFailedEventsAsync(
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get high severity events
    /// </summary>
    Task<IReadOnlyList<SecurityAuditLog>> GetHighSeverityEventsAsync(
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events that should trigger alerts
    /// </summary>
    Task<IReadOnlyList<SecurityAuditLog>> GetAlertableEventsAsync(
        DateTime? from = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed login attempts for user or IP
    /// </summary>
    Task<int> GetFailedLoginCountAsync(
        Guid? userId = null,
        string? ipAddress = null,
        TimeSpan? timeWindow = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old audit logs (for GDPR compliance)
    /// </summary>
    Task<int> DeleteOldLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
