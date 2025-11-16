using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Security.API.Services;

/// <summary>
/// Redis-based distributed rate limiting service
/// Uses Lua scripts for atomic operations
/// Thread-safe and supports distributed systems
/// </summary>
public class RedisRateLimitingService : IRateLimitingService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimitingService> _logger;
    private readonly IDatabase _database;

    // Lua script for atomic check and increment
    // Returns: remaining count if allowed, -1 if rate limit exceeded
    private const string LuaScript = @"
        local key = KEYS[1]
        local limit = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])
        local now = tonumber(ARGV[3])

        -- Remove expired entries (outside window)
        redis.call('ZREMRANGEBYSCORE', key, 0, now - window)

        -- Count current requests in window
        local current = redis.call('ZCARD', key)

        if current < limit then
            -- Add current request
            redis.call('ZADD', key, now, now)
            -- Set expiration on key (cleanup)
            redis.call('EXPIRE', key, window)
            -- Return remaining requests
            return limit - current - 1
        else
            -- Rate limit exceeded
            return -1
        end
    ";

    private readonly LuaScript _compiledScript;

    // Rate limit configurations: operation -> (window in seconds, limit)
    private readonly Dictionary<string, (int WindowSeconds, int Limit)> _rateLimits = new()
    {
        // Authentication
        ["login"] = (900, 5),              // 5 attempts per 15 minutes
        ["register"] = (3600, 3),          // 3 registrations per hour
        ["refresh_token"] = (300, 10),     // 10 refreshes per 5 minutes

        // Messaging
        ["send_message"] = (60, 60),       // 60 messages per minute
        ["send_group_message"] = (60, 30), // 30 group messages per minute
        ["create_conversation"] = (600, 10), // 10 conversations per 10 minutes

        // Groups
        ["create_group"] = (3600, 5),      // 5 groups per hour
        ["invite_to_group"] = (60, 20),    // 20 invites per minute

        // Files
        ["upload_file"] = (300, 10),       // 10 file uploads per 5 minutes
        ["download_file"] = (60, 30),      // 30 downloads per minute

        // User actions
        ["add_contact"] = (600, 20),       // 20 contacts per 10 minutes
        ["block_user"] = (600, 10),        // 10 blocks per 10 minutes
        ["update_profile"] = (300, 5),     // 5 profile updates per 5 minutes

        // Default
        ["default"] = (300, 30)            // 30 requests per 5 minutes
    };

    public RedisRateLimitingService(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimitingService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _redis.GetDatabase();
        _compiledScript = LuaScript.Prepare(LuaScript);
    }

    public bool AllowRequest(string key, string operation)
    {
        try
        {
            var (windowSeconds, limit) = GetRateLimit(operation);
            var redisKey = GetRedisKey(key, operation);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Execute Lua script atomically
            var result = (int)_database.ScriptEvaluate(
                _compiledScript,
                new RedisKey[] { redisKey },
                new RedisValue[] { limit, windowSeconds * 1000, now }  // Convert to milliseconds
            );

            var allowed = result >= 0;

            if (!allowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for key {Key}, operation {Operation}",
                    key,
                    operation);
            }
            else
            {
                _logger.LogDebug(
                    "Request allowed for key {Key}, operation {Operation}. Remaining: {Remaining}",
                    key,
                    operation,
                    result);
            }

            return allowed;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking rate limit for key {Key}, operation {Operation}. Allowing request as fallback.",
                key,
                operation);

            // Fail open - allow request if Redis has issues
            return true;
        }
    }

    public int GetRemainingRequests(string key, string operation)
    {
        try
        {
            var (windowSeconds, limit) = GetRateLimit(operation);
            var redisKey = GetRedisKey(key, operation);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (windowSeconds * 1000);

            // Remove expired entries
            _database.SortedSetRemoveRangeByScore(
                redisKey,
                0,
                windowStart);

            // Count current requests
            var current = (int)_database.SortedSetLength(redisKey);

            return Math.Max(0, limit - current);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting remaining requests for key {Key}, operation {Operation}",
                key,
                operation);

            return 0; // Conservative: assume no requests remaining
        }
    }

    public TimeSpan GetTimeUntilReset(string key, string operation)
    {
        try
        {
            var (windowSeconds, _) = GetRateLimit(operation);
            var redisKey = GetRedisKey(key, operation);

            // Get oldest entry in sorted set
            var oldest = _database.SortedSetRangeByRankWithScores(
                redisKey,
                start: 0,
                stop: 0);

            if (oldest.Length == 0)
            {
                return TimeSpan.Zero;
            }

            var oldestTimestamp = (long)oldest[0].Score;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var resetTime = oldestTimestamp + (windowSeconds * 1000);

            var timeUntilReset = resetTime - now;

            return timeUntilReset > 0
                ? TimeSpan.FromMilliseconds(timeUntilReset)
                : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting time until reset for key {Key}, operation {Operation}",
                key,
                operation);

            return TimeSpan.Zero;
        }
    }

    public void ResetLimit(string key, string operation)
    {
        try
        {
            var redisKey = GetRedisKey(key, operation);
            _database.KeyDelete(redisKey);

            _logger.LogInformation(
                "Rate limit reset for key {Key}, operation {Operation}",
                key,
                operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error resetting rate limit for key {Key}, operation {Operation}",
                key,
                operation);
        }
    }

    private string GetRedisKey(string key, string operation)
    {
        // Format: ratelimit:{operation}:{key}
        return $"ratelimit:{operation}:{key}";
    }

    private (int WindowSeconds, int Limit) GetRateLimit(string operation)
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
