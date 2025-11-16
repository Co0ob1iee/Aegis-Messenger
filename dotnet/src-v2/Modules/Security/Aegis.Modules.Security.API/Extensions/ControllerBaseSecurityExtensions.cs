using Aegis.Modules.Security.Application.Services;
using Aegis.Modules.Security.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Modules.Security.API.Extensions;

/// <summary>
/// Extension methods for ControllerBase to easily log security events
/// </summary>
public static class ControllerBaseSecurityExtensions
{
    /// <summary>
    /// Log successful security event from controller
    /// </summary>
    public static async Task LogSecuritySuccessAsync(
        this ControllerBase controller,
        ISecurityAuditService auditService,
        SecurityEventType eventType,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        var userId = controller.HttpContext.GetUserId();
        var ipAddress = controller.HttpContext.GetIpAddress();
        var userAgent = controller.HttpContext.GetUserAgent();

        await auditService.LogSuccessAsync(
            eventType,
            userId,
            ipAddress,
            userAgent,
            details,
            relatedEntityId,
            relatedEntityType);
    }

    /// <summary>
    /// Log failed security event from controller
    /// </summary>
    public static async Task LogSecurityFailureAsync(
        this ControllerBase controller,
        ISecurityAuditService auditService,
        SecurityEventType eventType,
        string errorMessage,
        string? details = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        var userId = controller.HttpContext.GetUserId();
        var ipAddress = controller.HttpContext.GetIpAddress();
        var userAgent = controller.HttpContext.GetUserAgent();

        await auditService.LogFailureAsync(
            eventType,
            errorMessage,
            userId,
            ipAddress,
            userAgent,
            details,
            relatedEntityId,
            relatedEntityType);
    }

    /// <summary>
    /// Get current user ID
    /// </summary>
    public static Guid? GetCurrentUserId(this ControllerBase controller)
    {
        return controller.HttpContext.GetUserId();
    }

    /// <summary>
    /// Get current user ID or throw if not authenticated
    /// </summary>
    public static Guid GetCurrentUserIdOrThrow(this ControllerBase controller)
    {
        var userId = controller.HttpContext.GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        return userId.Value;
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public static bool IsUserAuthenticated(this ControllerBase controller)
    {
        return controller.HttpContext.IsAuthenticated();
    }
}
