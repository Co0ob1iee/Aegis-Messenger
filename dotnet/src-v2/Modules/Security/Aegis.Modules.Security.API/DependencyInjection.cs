using Aegis.Modules.Security.API.Services;
using Aegis.Modules.Security.Application;
using Aegis.Modules.Security.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Aegis.Modules.Security.API;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Infrastructure
        services.AddSecurityInfrastructure(configuration);

        // Application layer (includes event handlers)
        services.AddSecurityApplication();

        // Rate Limiting - Redis or In-Memory
        AddRateLimiting(services, configuration);

        return services;
    }

    private static void AddRateLimiting(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConnection))
        {
            try
            {
                // Configure Redis
                var redis = ConnectionMultiplexer.Connect(redisConnection);
                services.AddSingleton<IConnectionMultiplexer>(redis);
                services.AddSingleton<IRateLimitingService, RedisRateLimitingService>();

                // Log that Redis is being used
                var logger = services.BuildServiceProvider()
                    .GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisRateLimitingService>>();
                logger.LogInformation("Rate limiting: Using Redis-based distributed implementation");
            }
            catch (Exception ex)
            {
                // Fallback to in-memory if Redis connection fails
                var logger = services.BuildServiceProvider()
                    .GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                    .CreateLogger("SecurityModule");
                logger.LogWarning(ex, "Failed to connect to Redis. Falling back to in-memory rate limiting.");
                services.AddSingleton<IRateLimitingService, RateLimitingService>();
            }
        }
        else
        {
            // Use in-memory implementation
            services.AddSingleton<IRateLimitingService, RateLimitingService>();

            var logger = services.BuildServiceProvider()
                .GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                .CreateLogger("SecurityModule");
            logger.LogInformation("Rate limiting: Using in-memory implementation (not suitable for multi-instance deployments)");
        }
    }
}
