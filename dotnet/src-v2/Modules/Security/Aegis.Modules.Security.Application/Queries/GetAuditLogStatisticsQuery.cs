using Aegis.Modules.Security.Application.DTOs;
using MediatR;

namespace Aegis.Modules.Security.Application.Queries;

/// <summary>
/// Query to get audit log statistics for dashboard
/// </summary>
public sealed record GetAuditLogStatisticsQuery : IRequest<AuditLogStatisticsDto>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int TopUsersLimit { get; init; } = 10;
    public int TopIpsLimit { get; init; } = 10;
}
