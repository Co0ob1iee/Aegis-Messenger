using Aegis.Modules.Security.Application.DTOs;
using Aegis.Modules.Security.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Security.Application.Queries.Handlers;

/// <summary>
/// Handler for GetAuditLogByIdQuery
/// </summary>
public sealed class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, SecurityAuditLogDto?>
{
    private readonly ISecurityAuditRepository _repository;

    public GetAuditLogByIdQueryHandler(ISecurityAuditRepository repository)
    {
        _repository = repository;
    }

    public async Task<SecurityAuditLogDto?> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var log = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (log == null)
            return null;

        return new SecurityAuditLogDto
        {
            Id = log.Id,
            EventType = log.EventType,
            Severity = log.Severity,
            Timestamp = log.Timestamp,
            IsSuccessful = log.IsSuccessful,
            UserId = log.UserId,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            ErrorMessage = log.ErrorMessage,
            Details = log.Details,
            RelatedEntityId = log.RelatedEntityId,
            RelatedEntityType = log.RelatedEntityType,
            RequestPath = log.RequestPath,
            RequestMethod = log.RequestMethod,
            ResponseStatusCode = log.ResponseStatusCode,
            RequestDuration = log.RequestDuration
        };
    }
}
