using Aegis.Modules.Security.Application.DTOs;
using Aegis.Modules.Security.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Security.Application.Queries.Handlers;

/// <summary>
/// Handler for GetUserAuditLogsQuery
/// </summary>
public sealed class GetUserAuditLogsQueryHandler : IRequestHandler<GetUserAuditLogsQuery, PagedResult<SecurityAuditLogDto>>
{
    private readonly ISecurityAuditRepository _repository;

    public GetUserAuditLogsQueryHandler(ISecurityAuditRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<SecurityAuditLogDto>> Handle(
        GetUserAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _repository.GetQueryable()
            .Where(x => x.UserId == request.UserId);

        if (request.From.HasValue)
            query = query.Where(x => x.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.Timestamp <= request.To.Value);

        query = query.OrderByDescending(x => x.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
            return PagedResult<SecurityAuditLogDto>.Empty(request.PageNumber, request.PageSize);

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
