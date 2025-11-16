using Aegis.Modules.Security.Domain.Entities;
using Aegis.Modules.Security.Domain.Enums;

namespace Aegis.Modules.Security.Application.Services;

/// <summary>
/// Service for logging security audit events
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Log successful security event
    /// </summary>
    Task LogSuccessAsync(
        SecurityEventType eventType,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log failed security event
    /// </summary>
    Task LogFailureAsync(
        SecurityEventType eventType,
        string errorMessage,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent audit logs for user
    /// </summary>
    Task<IReadOnlyList<SecurityAuditLog>> GetUserActivityAsync(
        Guid userId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security alerts
    /// </summary>
    Task<IReadOnlyList<SecurityAuditLog>> GetAlertsAsync(
        DateTime? from = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has excessive failed login attempts
    /// </summary>
    Task<bool> HasExcessiveFailedLoginsAsync(
        Guid? userId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
