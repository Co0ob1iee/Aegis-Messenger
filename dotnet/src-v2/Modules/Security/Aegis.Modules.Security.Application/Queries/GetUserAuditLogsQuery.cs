using Aegis.Modules.Security.Application.DTOs;
using MediatR;

namespace Aegis.Modules.Security.Application.Queries;

/// <summary>
/// Query to get audit logs for a specific user
/// </summary>
public sealed record GetUserAuditLogsQuery : IRequest<PagedResult<SecurityAuditLogDto>>
{
    public Guid UserId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
