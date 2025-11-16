using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Security.API.Services;

/// <summary>
/// In-memory sliding window rate limiter
/// For production, consider using Redis for distributed rate limiting
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requests = new();
    private readonly ILogger<RateLimitingService> _logger;

    // Rate limit configurations: operation -> (window, limit)
    private readonly Dictionary<string, (TimeSpan Window, int Limit)> _rateLimits = new()
    {
        // Authentication
        ["login"] = (TimeSpan.FromMinutes(15), 5),              // 5 attempts per 15 minutes
        ["register"] = (TimeSpan.FromHours(1), 3),              // 3 registrations per hour per IP
        ["refresh_token"] = (TimeSpan.FromMinutes(5), 10),      // 10 refreshes per 5 minutes

        // Messaging
        ["send_message"] = (TimeSpan.FromMinutes(1), 60),       // 60 messages per minute
        ["send_group_message"] = (TimeSpan.FromMinutes(1), 30), // 30 group messages per minute
        ["create_conversation"] = (TimeSpan.FromMinutes(10), 10), // 10 conversations per 10 minutes

        // Groups
        ["create_group"] = (TimeSpan.FromHours(1), 5),          // 5 groups per hour
        ["invite_to_group"] = (TimeSpan.FromMinutes(1), 20),    // 20 invites per minute

        // Files
        ["upload_file"] = (TimeSpan.FromMinutes(5), 10),        // 10 file uploads per 5 minutes
        ["download_file"] = (TimeSpan.FromMinutes(1), 30),      // 30 downloads per minute

        // User actions
        ["add_contact"] = (TimeSpan.FromMinutes(10), 20),       // 20 contacts per 10 minutes
        ["block_user"] = (TimeSpan.FromMinutes(10), 10),        // 10 blocks per 10 minutes
        ["update_profile"] = (TimeSpan.FromMinutes(5), 5),      // 5 profile updates per 5 minutes

        // Default
        ["default"] = (TimeSpan.FromMinutes(5), 30)             // 30 requests per 5 minutes
    };

    public RateLimitingService(ILogger<RateLimitingService> logger)
    {
        _logger = logger;
    }

    public bool AllowRequest(string key, string operation)
    {
        var rateLimitKey = $"{key}:{operation}";
        var (window, limit) = GetRateLimit(operation);

        if (!_requests.TryGetValue(rateLimitKey, out var queue))
        {
            queue = new Queue<DateTime>();
            _requests[rateLimitKey] = queue;
        }

        lock (queue)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - window;

            // Remove old requests outside the window
            while (queue.Count > 0 && queue.Peek() < windowStart)
            {
                queue.Dequeue();
            }

            // Check if limit exceeded
            if (queue.Count >= limit)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for key {Key}, operation {Operation}. " +
                    "Current: {Current}, Limit: {Limit}, Window: {Window}",
                    key,
                    operation,
                    queue.Count,
                    limit,
                    window);

                return false;
            }

            // Add current request
            queue.Enqueue(now);

            _logger.LogDebug(
                "Request allowed for key {Key}, operation {Operation}. " +
                "Current: {Current}/{Limit}",
                key,
                operation,
                queue.Count,
                limit);

            return true;
        }
    }

    public int GetRemainingRequests(string key, string operation)
    {
        var rateLimitKey = $"{key}:{operation}";
        var (window, limit) = GetRateLimit(operation);

        if (!_requests.TryGetValue(rateLimitKey, out var queue))
        {
            return limit;
        }

        lock (queue)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - window;

            // Remove old requests outside the window
            while (queue.Count > 0 && queue.Peek() < windowStart)
            {
                queue.Dequeue();
            }

            return Math.Max(0, limit - queue.Count);
        }
    }

    public TimeSpan GetTimeUntilReset(string key, string operation)
    {
        var rateLimitKey = $"{key}:{operation}";
        var (window, _) = GetRateLimit(operation);

        if (!_requests.TryGetValue(rateLimitKey, out var queue) || queue.Count == 0)
        {
            return TimeSpan.Zero;
        }

        lock (queue)
        {
            var now = DateTime.UtcNow;
            var oldest = queue.Peek();
            var resetTime = oldest + window;

            return resetTime > now ? resetTime - now : TimeSpan.Zero;
        }
    }

    public void ResetLimit(string key, string operation)
    {
        var rateLimitKey = $"{key}:{operation}";
        _requests.TryRemove(rateLimitKey, out _);

        _logger.LogInformation(
            "Rate limit reset for key {Key}, operation {Operation}",
            key,
            operation);
    }

    private (TimeSpan Window, int Limit) GetRateLimit(string operation)
    {
        if (_rateLimits.TryGetValue(operation, out var limit))
        {
            return limit;
        }

        _logger.LogWarning(
            "No rate limit configured for operation {Operation}, using default",
            operation);

        return _rateLimits["default"];
    }
}
