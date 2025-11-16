using Aegis.Modules.Security.API.Services;
using Aegis.Modules.Security.Application;
using Aegis.Modules.Security.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // API services
        services.AddSingleton<IRateLimitingService, RateLimitingService>();

        return services;
    }
}
