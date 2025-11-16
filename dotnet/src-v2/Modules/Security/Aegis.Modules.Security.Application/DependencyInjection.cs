using Aegis.Modules.Security.Application.EventHandlers;
using Aegis.Modules.Security.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Security.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityApplication(this IServiceCollection services)
    {
        // Services
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();

        // Event handlers - automatically registered by MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}
