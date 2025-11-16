using Aegis.Modules.Security.Application.DTOs;
using Aegis.Modules.Security.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Security.Application.Queries.Handlers;

/// <summary>
/// Handler for GetAuditLogsQuery
/// </summary>
public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<SecurityAuditLogDto>>
{
    private readonly ISecurityAuditRepository _repository;

    public GetAuditLogsQueryHandler(ISecurityAuditRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<SecurityAuditLogDto>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        // Get queryable
        var query = _repository.GetQueryable();

        // Apply filters
        if (request.UserId.HasValue)
            query = query.Where(x => x.UserId == request.UserId.Value);

        if (!string.IsNullOrEmpty(request.IpAddress))
            query = query.Where(x => x.IpAddress == request.IpAddress);

        if (request.EventType.HasValue)
            query = query.Where(x => x.EventType == request.EventType.Value);

        if (request.Severity.HasValue)
            query = query.Where(x => x.Severity == request.Severity.Value);

        if (request.IsSuccessful.HasValue)
            query = query.Where(x => x.IsSuccessful == request.IsSuccessful.Value);

        if (request.From.HasValue)
            query = query.Where(x => x.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.Timestamp <= request.To.Value);

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "eventtype" => request.SortDescending
                ? query.OrderByDescending(x => x.EventType)
                : query.OrderBy(x => x.EventType),
            "severity" => request.SortDescending
                ? query.OrderByDescending(x => x.Severity)
                : query.OrderBy(x => x.Severity),
            "userid" => request.SortDescending
                ? query.OrderByDescending(x => x.UserId)
                : query.OrderBy(x => x.UserId),
            _ => request.SortDescending
                ? query.OrderByDescending(x => x.Timestamp)
                : query.OrderBy(x => x.Timestamp)
        };

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
            return PagedResult<SecurityAuditLogDto>.Empty(request.PageNumber, request.PageSize);

        // Apply pagination
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SecurityAuditLogDto
            {
                Id = x.Id,
                EventType = x.EventType,
                Severity = x.Severity,
                Timestamp = x.Timestamp,
                IsSuccessful = x.IsSuccessful,
                UserId = x.UserId,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                ErrorMessage = x.ErrorMessage,
                Details = x.Details,
                RelatedEntityId = x.RelatedEntityId,
                RelatedEntityType = x.RelatedEntityType,
                RequestPath = x.RequestPath,
                RequestMethod = x.RequestMethod,
                ResponseStatusCode = x.ResponseStatusCode,
                RequestDuration = x.RequestDuration
            })
            .ToListAsync(cancellationToken);

        return PagedResult<SecurityAuditLogDto>.Create(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
