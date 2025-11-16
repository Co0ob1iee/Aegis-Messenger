using Aegis.Modules.Messages.API.Hubs;
using Aegis.Modules.Messages.Application;
using Aegis.Modules.Messages.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Messages.API;

/// <summary>
/// Dependency injection configuration for Messages API layer
/// Aggregates all Messages module dependencies
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMessagesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add all layers
        services.AddMessagesApplication();
        services.AddMessagesInfrastructure(configuration);

        // SignalR
        services.AddSignalR();

        return services;
    }

    public static void MapMessagesHub(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.MapHub<MessagingHub>("/hubs/messaging");
    }
}
