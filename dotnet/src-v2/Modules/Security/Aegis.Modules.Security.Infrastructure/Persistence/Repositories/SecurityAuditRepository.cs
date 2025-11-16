using Aegis.Modules.Security.Domain.Entities;
using Aegis.Modules.Security.Domain.Enums;
using Aegis.Modules.Security.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Security.Infrastructure.Persistence.Repositories;

public class SecurityAuditRepository : ISecurityAuditRepository
{
    private readonly SecurityDbContext _context;

    public SecurityAuditRepository(SecurityDbContext context)
    {
        _context = context;
    }

    public IQueryable<SecurityAuditLog> GetQueryable()
    {
        return _context.SecurityAuditLogs.AsQueryable();
    }

    public async Task<SecurityAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityAuditLogs
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    public async Task<SecurityAuditLog> AddAsync(SecurityAuditLog log, CancellationToken cancellationToken = default)
    {
        await _context.SecurityAuditLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<IReadOnlyList<SecurityAuditLog>> GetUserLogsAsync(
        Guid userId,
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Where(log => log.UserId == userId);

        if (from.HasValue)
            query = query.Where(log => log.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.Timestamp <= to.Value);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAuditLog>> GetByEventTypeAsync(
        SecurityEventType eventType,
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Where(log => log.EventType == eventType);

        if (from.HasValue)
            query = query.Where(log => log.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.Timestamp <= to.Value);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAuditLog>> GetFailedEventsAsync(
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Where(log => !log.IsSuccessful);

        if (from.HasValue)
            query = query.Where(log => log.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.Timestamp <= to.Value);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAuditLog>> GetHighSeverityEventsAsync(
        int limit = 100,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Where(log => log.Severity >= SecurityEventSeverity.High);

        if (from.HasValue)
            query = query.Where(log => log.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.Timestamp <= to.Value);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAuditLog>> GetAlertableEventsAsync(
        DateTime? from = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Where(log => log.Severity >= SecurityEventSeverity.High ||
                         (!log.IsSuccessful && log.Severity >= SecurityEventSeverity.Medium));

        if (from.HasValue)
            query = query.Where(log => log.Timestamp >= from.Value);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetFailedLoginCountAsync(
        Guid? userId = null,
        string? ipAddress = null,
        TimeSpan? timeWindow = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityAuditLogs
            .Where(log => log.EventType == SecurityEventType.LoginFailed &&
                         !log.IsSuccessful);

        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);

        if (!string.IsNullOrEmpty(ipAddress))
            query = query.Where(log => log.IpAddress == ipAddress);

        if (timeWindow.HasValue)
        {
            var from = DateTime.UtcNow - timeWindow.Value;
            query = query.Where(log => log.Timestamp >= from);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> DeleteOldLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var oldLogs = await _context.SecurityAuditLogs
            .Where(log => log.Timestamp < olderThan)
            .ToListAsync(cancellationToken);

        _context.SecurityAuditLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync(cancellationToken);

        return oldLogs.Count;
    }
}
