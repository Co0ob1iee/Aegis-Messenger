using Aegis.Modules.Security.Application.DTOs;
using MediatR;

namespace Aegis.Modules.Security.Application.Queries;

/// <summary>
/// Query to get a single audit log by ID
/// </summary>
public sealed record GetAuditLogByIdQuery(Guid Id) : IRequest<SecurityAuditLogDto?>;
