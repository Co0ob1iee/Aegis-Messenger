using Aegis.Modules.Security.Domain.Entities;
using Aegis.Modules.Security.Domain.Enums;
using Aegis.Modules.Security.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Security.Application.Services;

/// <summary>
/// Service for logging and querying security audit events
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ISecurityAuditRepository _repository;
    private readonly ILogger<SecurityAuditService> _logger;

    // Thresholds for security alerts
    private const int MaxFailedLoginsPerHour = 5;
    private const int MaxFailedLoginsPerDay = 20;

    public SecurityAuditService(
        ISecurityAuditRepository repository,
        ILogger<SecurityAuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task LogSuccessAsync(
        SecurityEventType eventType,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateSuccess(
            eventType,
            userId,
            ipAddress,
            userAgent,
            details,
            relatedEntityId,
            relatedEntityType);

        await _repository.AddAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "Security event logged: {EventType} for user {UserId} from {IpAddress}",
            eventType,
            userId,
            ipAddress);

        // Check if we should alert
        if (auditLog.ShouldAlert())
        {
            _logger.LogWarning(
                "SECURITY ALERT: {EventType} - {Details}",
                eventType,
                details ?? "No details");
        }
    }

    public async Task LogFailureAsync(
        SecurityEventType eventType,
        string errorMessage,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = SecurityAuditLog.CreateFailure(
            eventType,
            errorMessage,
            userId,
            ipAddress,
            userAgent,
            details,
            relatedEntityId,
            relatedEntityType);

        await _repository.AddAsync(auditLog, cancellationToken);

        _logger.LogWarning(
            "Security failure logged: {EventType} for user {UserId} from {IpAddress} - {Error}",
            eventType,
            userId,
            ipAddress,
            errorMessage);

        // Check if we should alert
        if (auditLog.ShouldAlert())
        {
            _logger.LogError(
                "SECURITY ALERT: {EventType} - {Error}",
                eventType,
                errorMessage);
        }

        // Check for brute force attacks
        if (eventType == SecurityEventType.LoginFailed)
        {
            var isExcessive = await HasExcessiveFailedLoginsAsync(userId, ipAddress, cancellationToken);
            if (isExcessive)
            {
                _logger.LogCritical(
                    "POSSIBLE BRUTE FORCE ATTACK detected for user {UserId} from {IpAddress}",
                    userId,
                    ipAddress);

                // Log suspicious activity
                await LogSuccessAsync(
                    SecurityEventType.SuspiciousActivity,
                    userId,
                    ipAddress,
                    userAgent,
                    $"Excessive failed login attempts detected",
                    cancellationToken: cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<SecurityAuditLog>> GetUserActivityAsync(
        Guid userId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetUserLogsAsync(
            userId,
            limit,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAuditLog>> GetAlertsAsync(
        DateTime? from = null,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetAlertableEventsAsync(from, cancellationToken);
    }

    public async Task<bool> HasExcessiveFailedLoginsAsync(
        Guid? userId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // Check last hour
        var failedLastHour = await _repository.GetFailedLoginCountAsync(
            userId,
            ipAddress,
            TimeSpan.FromHours(1),
            cancellationToken);

        if (failedLastHour >= MaxFailedLoginsPerHour)
        {
            return true;
        }

        // Check last 24 hours
        var failedLastDay = await _repository.GetFailedLoginCountAsync(
            userId,
            ipAddress,
            TimeSpan.FromDays(1),
            cancellationToken);

        return failedLastDay >= MaxFailedLoginsPerDay;
    }
}
