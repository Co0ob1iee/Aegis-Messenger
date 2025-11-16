using Aegis.Modules.Security.Application.DTOs;
using Aegis.Modules.Security.Domain.Enums;
using MediatR;

namespace Aegis.Modules.Security.Application.Queries;

/// <summary>
/// Query to get paginated audit logs with filtering
/// </summary>
public sealed record GetAuditLogsQuery : IRequest<PagedResult<SecurityAuditLogDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    // Filters
    public Guid? UserId { get; init; }
    public string? IpAddress { get; init; }
    public SecurityEventType? EventType { get; init; }
    public SecurityEventSeverity? Severity { get; init; }
    public bool? IsSuccessful { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }

    // Sorting
    public string? SortBy { get; init; } = "Timestamp";
    public bool SortDescending { get; init; } = true;
}
