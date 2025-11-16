using Aegis.Modules.Users.Application;
using Aegis.Modules.Users.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Users.API;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddUsersApplication();
        services.AddUsersInfrastructure(configuration);

        return services;
    }
}
