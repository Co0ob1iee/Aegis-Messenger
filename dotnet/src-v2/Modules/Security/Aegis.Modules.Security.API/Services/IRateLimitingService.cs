namespace Aegis.Modules.Security.API.Services;

/// <summary>
/// Service for rate limiting requests
/// Prevents abuse and brute force attacks
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Check if request is allowed based on rate limits
    /// </summary>
    /// <param name="key">Unique key for rate limiting (userId, IP, etc.)</param>
    /// <param name="operation">Operation name (login, send_message, etc.)</param>
    /// <returns>True if request is allowed, false if rate limit exceeded</returns>
    bool AllowRequest(string key, string operation);

    /// <summary>
    /// Get remaining requests in current window
    /// </summary>
    int GetRemainingRequests(string key, string operation);

    /// <summary>
    /// Get time until rate limit resets
    /// </summary>
    TimeSpan GetTimeUntilReset(string key, string operation);

    /// <summary>
    /// Reset rate limit for a key
    /// </summary>
    void ResetLimit(string key, string operation);
}
