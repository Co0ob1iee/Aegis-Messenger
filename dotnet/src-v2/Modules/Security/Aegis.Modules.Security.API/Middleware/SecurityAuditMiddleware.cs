using System.Security.Claims;
using Aegis.Modules.Security.Application.Services;
using Aegis.Modules.Security.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Security.API.Middleware;

/// <summary>
/// Middleware for automatic security audit logging of HTTP requests
/// Logs all API requests with user, IP, and timing information
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(
        RequestDelegate next,
        ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISecurityAuditService auditService)
    {
        // Skip audit logging for health checks and swagger
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var userId = GetUserId(context);
        var ipAddress = GetIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var path = context.Request.Path.ToString();
        var method = context.Request.Method;

        Exception? exception = null;

        try
        {
            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            var statusCode = context.Response.StatusCode;

            // Determine if request was successful
            var isSuccess = statusCode >= 200 && statusCode < 400;

            // Log based on route and status
            if (ShouldAuditRequest(path, method, statusCode))
            {
                await LogRequestAsync(
                    auditService,
                    path,
                    method,
                    statusCode,
                    isSuccess,
                    userId,
                    ipAddress,
                    userAgent,
                    duration,
                    exception);
            }
        }
    }

    private bool ShouldAuditRequest(string path, string method, int statusCode)
    {
        // Always audit authentication endpoints
        if (path.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
            return true;

        // Always audit failed requests
        if (statusCode >= 400)
            return true;

        // Audit create/update/delete operations
        if (method is "POST" or "PUT" or "PATCH" or "DELETE")
            return true;

        // Skip GET requests that succeeded (too noisy)
        if (method == "GET" && statusCode < 400)
            return false;

        return true;
    }

    private async Task LogRequestAsync(
        ISecurityAuditService auditService,
        string path,
        string method,
        int statusCode,
        bool isSuccess,
        Guid? userId,
        string ipAddress,
        string userAgent,
        TimeSpan duration,
        Exception? exception)
    {
        // Determine event type from path
        var eventType = DetermineEventType(path, method, statusCode);

        var details = $"{method} {path} → {statusCode} ({duration.TotalMilliseconds:F0}ms)";

        if (isSuccess)
        {
            await auditService.LogSuccessAsync(
                eventType,
                userId,
                ipAddress,
                userAgent,
                details);
        }
        else
        {
            var errorMessage = exception?.Message ?? $"HTTP {statusCode}";

            await auditService.LogFailureAsync(
                eventType,
                errorMessage,
                userId,
                ipAddress,
                userAgent,
                details);
        }
    }

    private SecurityEventType DetermineEventType(string path, string method, int statusCode)
    {
        var pathLower = path.ToLowerInvariant();

        // Authentication events
        if (pathLower.Contains("/auth/login"))
            return statusCode < 400 ? SecurityEventType.LoginSuccess : SecurityEventType.LoginFailed;

        if (pathLower.Contains("/auth/logout"))
            return SecurityEventType.Logout;

        if (pathLower.Contains("/auth/register"))
            return SecurityEventType.AccountCreated;

        // Message events
        if (pathLower.Contains("/messages"))
        {
            return method switch
            {
                "POST" => SecurityEventType.MessageSent,
                "DELETE" => SecurityEventType.MessageDeleted,
                _ => SecurityEventType.MessageSent
            };
        }

        // Group events
        if (pathLower.Contains("/groups"))
        {
            if (method == "POST" && pathLower.Contains("/create"))
                return SecurityEventType.GroupCreated;

            if (method == "DELETE")
                return SecurityEventType.GroupDeleted;

            if (pathLower.Contains("/join"))
                return SecurityEventType.UserJoinedGroup;

            if (pathLower.Contains("/leave"))
                return SecurityEventType.UserLeftGroup;
        }

        // File events
        if (pathLower.Contains("/files"))
        {
            if (method == "POST" || pathLower.Contains("/upload"))
                return SecurityEventType.FileUploaded;

            if (method == "GET" || pathLower.Contains("/download"))
                return SecurityEventType.FileDownloaded;

            if (method == "DELETE")
                return SecurityEventType.FileDeleted;
        }

        // Privacy events
        if (pathLower.Contains("/privacy") || pathLower.Contains("/settings"))
            return SecurityEventType.PrivacySettingsChanged;

        // User blocking
        if (pathLower.Contains("/block"))
            return SecurityEventType.UserBlocked;

        if (pathLower.Contains("/unblock"))
            return SecurityEventType.UserUnblocked;

        // Unauthorized access
        if (statusCode == 401 || statusCode == 403)
            return SecurityEventType.UnauthorizedAccess;

        // Default
        return SecurityEventType.MessageSent;  // Generic event
    }

    private Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    private string GetIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Extension methods for registering SecurityAuditMiddleware
/// </summary>
public static class SecurityAuditMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityAudit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityAuditMiddleware>();
    }
}
