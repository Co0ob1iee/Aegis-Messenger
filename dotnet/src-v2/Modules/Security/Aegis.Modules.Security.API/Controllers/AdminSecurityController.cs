using Aegis.Modules.Security.Application.DTOs;
using Aegis.Modules.Security.Application.Queries;
using Aegis.Modules.Security.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Modules.Security.API.Controllers;

/// <summary>
/// Admin endpoints for security audit logs
/// </summary>
[ApiController]
[Route("api/admin/security")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminSecurityController : ControllerBase
{
    private readonly ISender _sender;

    public AdminSecurityController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get paginated audit logs with filtering
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResult<SecurityAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SecurityAuditLogDto>>> GetAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] SecurityEventType? eventType = null,
        [FromQuery] SecurityEventSeverity? severity = null,
        [FromQuery] bool? isSuccessful = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? sortBy = "Timestamp",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var query = new GetAuditLogsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            UserId = userId,
            IpAddress = ipAddress,
            EventType = eventType,
            Severity = severity,
            IsSuccessful = isSuccessful,
            From = from,
            To = to,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get single audit log by ID
    /// </summary>
    [HttpGet("audit-logs/{id:guid}")]
    [ProducesResponseType(typeof(SecurityAuditLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SecurityAuditLogDto>> GetAuditLogById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAuditLogByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Audit log with ID {id} not found." });

        return Ok(result);
    }

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    [HttpGet("audit-logs/user/{userId:guid}")]
    [ProducesResponseType(typeof(PagedResult<SecurityAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SecurityAuditLogDto>>> GetUserAuditLogs(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var query = new GetUserAuditLogsQuery
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            From = from,
            To = to
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get audit log statistics for dashboard
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditLogStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogStatisticsDto>> GetStatistics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int topUsersLimit = 10,
        [FromQuery] int topIpsLimit = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate limits
        if (topUsersLimit < 1) topUsersLimit = 10;
        if (topUsersLimit > 50) topUsersLimit = 50;
        if (topIpsLimit < 1) topIpsLimit = 10;
        if (topIpsLimit > 50) topIpsLimit = 50;

        var query = new GetAuditLogStatisticsQuery
        {
            From = from,
            To = to,
            TopUsersLimit = topUsersLimit,
            TopIpsLimit = topIpsLimit
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Export audit logs to CSV
    /// </summary>
    [HttpGet("audit-logs/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] SecurityEventType? eventType = null,
        [FromQuery] SecurityEventSeverity? severity = null,
        [FromQuery] bool? isSuccessful = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        // Get all matching logs (limited to 10000 for performance)
        var query = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = 10000,
            UserId = userId,
            IpAddress = ipAddress,
            EventType = eventType,
            Severity = severity,
            IsSuccessful = isSuccessful,
            From = from,
            To = to,
            SortBy = "Timestamp",
            SortDescending = true
        };

        var result = await _sender.Send(query, cancellationToken);

        // Generate CSV
        var csv = GenerateCsv(result.Items);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

        return File(
            bytes,
            "text/csv",
            $"audit-logs-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.csv");
    }

    private static string GenerateCsv(IEnumerable<SecurityAuditLogDto> logs)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("Id,Timestamp,EventType,Severity,IsSuccessful,UserId,Username,IpAddress,UserAgent,ErrorMessage,Details,RequestPath,RequestMethod,ResponseStatusCode,RequestDuration");

        // Rows
        foreach (var log in logs)
        {
            sb.AppendLine($"\"{log.Id}\"," +
                         $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                         $"\"{log.EventType}\"," +
                         $"\"{log.Severity}\"," +
                         $"\"{log.IsSuccessful}\"," +
                         $"\"{log.UserId}\"," +
                         $"\"{EscapeCsv(log.Username)}\"," +
                         $"\"{EscapeCsv(log.IpAddress)}\"," +
                         $"\"{EscapeCsv(log.UserAgent)}\"," +
                         $"\"{EscapeCsv(log.ErrorMessage)}\"," +
                         $"\"{EscapeCsv(log.Details)}\"," +
                         $"\"{EscapeCsv(log.RequestPath)}\"," +
                         $"\"{EscapeCsv(log.RequestMethod)}\"," +
                         $"\"{log.ResponseStatusCode}\"," +
                         $"\"{log.RequestDuration?.TotalMilliseconds}\"");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\"", "\"\"");
    }
}
