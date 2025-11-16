using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Aegis.Modules.Security.API.Extensions;

/// <summary>
/// Extension methods for HttpContext to easily retrieve security-related information
/// </summary>
public static class HttpContextSecurityExtensions
{
    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    public static Guid? GetUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst("userId")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Get client IP address, accounting for proxies and load balancers
    /// </summary>
    public static string GetIpAddress(this HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Check for CF-Connecting-IP (Cloudflare)
        var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfConnectingIp))
        {
            return cfConnectingIp;
        }

        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Get user agent string
    /// </summary>
    public static string GetUserAgent(this HttpContext context)
    {
        return context.Request.Headers.UserAgent.ToString() ?? "unknown";
    }

    /// <summary>
    /// Get request path
    /// </summary>
    public static string GetRequestPath(this HttpContext context)
    {
        return context.Request.Path.ToString();
    }

    /// <summary>
    /// Get HTTP method
    /// </summary>
    public static string GetHttpMethod(this HttpContext context)
    {
        return context.Request.Method;
    }

    /// <summary>
    /// Check if request is authenticated
    /// </summary>
    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// Get username from claims
    /// </summary>
    public static string? GetUsername(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Name)?.Value
            ?? context.User.FindFirst("username")?.Value
            ?? context.User.FindFirst("preferred_username")?.Value;
    }

    /// <summary>
    /// Get email from claims
    /// </summary>
    public static string? GetEmail(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Email)?.Value
            ?? context.User.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Check if user has specific role
    /// </summary>
    public static bool HasRole(this HttpContext context, string role)
    {
        return context.User.IsInRole(role);
    }

    /// <summary>
    /// Get all user roles
    /// </summary>
    public static IEnumerable<string> GetRoles(this HttpContext context)
    {
        return context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    /// <summary>
    /// Get request fingerprint (for tracking purposes)
    /// Combines IP, User-Agent, and other headers
    /// </summary>
    public static string GetRequestFingerprint(this HttpContext context)
    {
        var components = new[]
        {
            context.GetIpAddress(),
            context.GetUserAgent(),
            context.Request.Headers.AcceptLanguage.ToString(),
            context.Request.Headers.Accept.ToString()
        };

        var combined = string.Join("|", components.Where(c => !string.IsNullOrEmpty(c)));

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));

        return Convert.ToBase64String(hash);
    }
}
