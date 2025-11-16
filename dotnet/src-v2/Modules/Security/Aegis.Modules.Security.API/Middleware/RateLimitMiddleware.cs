using System.Security.Claims;
using Aegis.Modules.Security.API.Services;
using Aegis.Modules.Security.Application.Services;
using Aegis.Modules.Security.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Security.API.Middleware;

/// <summary>
/// Middleware for automatic rate limiting of HTTP requests
/// Checks rate limits before processing request
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // Mapping of HTTP routes to rate limit operations
    private static readonly Dictionary<string, string> RouteToOperationMap = new()
    {
        // Auth routes
        ["/api/auth/login"] = "login",
        ["/api/auth/register"] = "register",
        ["/api/auth/refresh"] = "refresh_token",

        // Messages routes
        ["/api/messages/send"] = "send_message",
        ["/api/messages/group"] = "send_group_message",

        // Groups routes
        ["/api/groups/create"] = "create_group",
        ["/api/groups/invite"] = "invite_to_group",

        // Files routes
        ["/api/files/upload"] = "upload_file",
        ["/api/files/download"] = "download_file",

        // Users routes
        ["/api/users/contacts"] = "add_contact",
        ["/api/users/block"] = "block_user",
        ["/api/users/profile"] = "update_profile",
    };

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRateLimitingService rateLimiting,
        ISecurityAuditService auditService)
    {
        // Skip rate limiting for health checks and non-API routes
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Determine operation from route
        var operation = DetermineOperation(context.Request.Path, context.Request.Method);

        // Get rate limit key (user ID or IP address)
        var rateLimitKey = GetRateLimitKey(context);

        // Check rate limit
        if (!rateLimiting.AllowRequest(rateLimitKey, operation))
        {
            // Rate limit exceeded
            var remaining = rateLimiting.GetRemainingRequests(rateLimitKey, operation);
            var resetTime = rateLimiting.GetTimeUntilReset(rateLimitKey, operation);

            _logger.LogWarning(
                "Rate limit exceeded for {Key} on operation {Operation}. " +
                "Reset in {ResetTime}",
                rateLimitKey,
                operation,
                resetTime);

            // Log security event
            var userId = GetUserId(context);
            var ipAddress = GetIpAddress(context);

            await auditService.LogFailureAsync(
                SecurityEventType.RateLimitExceeded,
                $"Rate limit exceeded for operation: {operation}",
                userId,
                ipAddress,
                context.Request.Headers.UserAgent.ToString(),
                $"Remaining: {remaining}, Reset in: {resetTime}");

            // Return 429 Too Many Requests
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)resetTime.TotalSeconds).ToString();
            context.Response.Headers["X-RateLimit-Limit"] = "See documentation";
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((int)resetTime.TotalSeconds).ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Too many requests for operation '{operation}'. Please try again later.",
                retryAfter = (int)resetTime.TotalSeconds,
                remaining = remaining
            });

            return;
        }

        // Add rate limit headers to response
        var remainingRequests = rateLimiting.GetRemainingRequests(rateLimitKey, operation);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Remaining"] = remainingRequests.ToString();
            return Task.CompletedTask;
        });

        // Continue to next middleware
        await _next(context);
    }

    private string DetermineOperation(PathString path, string method)
    {
        // Try exact match first
        var pathString = path.Value?.ToLowerInvariant() ?? string.Empty;

        if (RouteToOperationMap.TryGetValue(pathString, out var operation))
        {
            return operation;
        }

        // Try prefix matching
        foreach (var (route, op) in RouteToOperationMap)
        {
            if (pathString.StartsWith(route, StringComparison.OrdinalIgnoreCase))
            {
                return op;
            }
        }

        // Default operation based on HTTP method
        return method.ToUpperInvariant() switch
        {
            "POST" => "create",
            "PUT" or "PATCH" => "update",
            "DELETE" => "delete",
            "GET" => "read",
            _ => "default"
        };
    }

    private string GetRateLimitKey(HttpContext context)
    {
        // Prefer user ID if authenticated
        var userId = GetUserId(context);
        if (userId.HasValue)
        {
            return $"user:{userId.Value}";
        }

        // Fall back to IP address
        var ipAddress = GetIpAddress(context);
        return $"ip:{ipAddress}";
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
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Use remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Extension methods for registering RateLimitMiddleware
/// </summary>
public static class RateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMiddleware>();
    }
}
