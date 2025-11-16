using Aegis.Modules.Security.Application.DTOs;
using Aegis.Modules.Security.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Security.Application.Queries.Handlers;

/// <summary>
/// Handler for GetAuditLogStatisticsQuery
/// </summary>
public sealed class GetAuditLogStatisticsQueryHandler : IRequestHandler<GetAuditLogStatisticsQuery, AuditLogStatisticsDto>
{
    private readonly ISecurityAuditRepository _repository;

    public GetAuditLogStatisticsQueryHandler(ISecurityAuditRepository repository)
    {
        _repository = repository;
    }

    public async Task<AuditLogStatisticsDto> Handle(
        GetAuditLogStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _repository.GetQueryable();

        if (request.From.HasValue)
            query = query.Where(x => x.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.Timestamp <= request.To.Value);

        // Get all logs for statistics (be careful with large datasets)
        var logs = await query.ToListAsync(cancellationToken);

        // Calculate statistics
        var totalEvents = logs.Count;
        var totalFailures = logs.Count(x => !x.IsSuccessful);
        var criticalEvents = logs.Count(x => x.Severity == Domain.Enums.SecurityEventSeverity.Critical);
        var highSeverityEvents = logs.Count(x => x.Severity == Domain.Enums.SecurityEventSeverity.High);

        // Event type counts
        var eventTypeCounts = logs
            .GroupBy(x => x.EventType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Severity counts
        var severityCounts = logs
            .GroupBy(x => x.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        // Top active users
        var topActiveUsers = logs
            .Where(x => x.UserId.HasValue)
            .GroupBy(x => x.UserId!.Value)
            .Select(g => new AuditLogStatisticsDto.TopUserActivity
            {
                UserId = g.Key,
                EventCount = g.Count(),
                FailureCount = g.Count(x => !x.IsSuccessful)
            })
            .OrderByDescending(x => x.EventCount)
            .Take(request.TopUsersLimit)
            .ToList();

        // Top IP addresses
        var topIpAddresses = logs
            .Where(x => !string.IsNullOrEmpty(x.IpAddress))
            .GroupBy(x => x.IpAddress!)
            .Select(g => new AuditLogStatisticsDto.TopIpActivity
            {
                IpAddress = g.Key,
                EventCount = g.Count(),
                FailureCount = g.Count(x => !x.IsSuccessful)
            })
            .OrderByDescending(x => x.EventCount)
            .Take(request.TopIpsLimit)
            .ToList();

        // Time range
        var oldestEvent = logs.Count > 0 ? logs.Min(x => x.Timestamp) : (DateTime?)null;
        var newestEvent = logs.Count > 0 ? logs.Max(x => x.Timestamp) : (DateTime?)null;

        return new AuditLogStatisticsDto
        {
            TotalEvents = totalEvents,
            TotalFailures = totalFailures,
            CriticalEvents = criticalEvents,
            HighSeverityEvents = highSeverityEvents,
            EventTypeCounts = eventTypeCounts,
            SeverityCounts = severityCounts,
            TopActiveUsers = topActiveUsers,
            TopIpAddresses = topIpAddresses,
            OldestEvent = oldestEvent,
            NewestEvent = newestEvent
        };
    }
}
