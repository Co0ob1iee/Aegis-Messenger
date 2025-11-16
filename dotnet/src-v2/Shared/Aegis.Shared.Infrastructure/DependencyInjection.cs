using Aegis.Shared.Infrastructure.Caching;
using Aegis.Shared.Infrastructure.EventBus;
using Aegis.Shared.Infrastructure.Exceptions;
using Aegis.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Shared.Infrastructure;

/// <summary>
/// Extension methods for registering shared infrastructure services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add shared infrastructure services to the service collection
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        // Caching
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Event Bus
        services.AddScoped<IEventBus, InMemoryEventBus>();

        // Exception Handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Add Unit of Work for a specific DbContext
    /// </summary>
    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        services.AddScoped<IUnitOfWork>(sp =>
        {
            var context = sp.GetRequiredService<TContext>();
            return new UnitOfWork(context);
        });

        return services;
    }
}
