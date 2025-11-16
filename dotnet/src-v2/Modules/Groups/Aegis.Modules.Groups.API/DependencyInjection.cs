using Aegis.Modules.Groups.Application;
using Aegis.Modules.Groups.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Groups.API;

public static class DependencyInjection
{
    public static IServiceCollection AddGroupsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGroupsApplication();
        services.AddGroupsInfrastructure(configuration);

        return services;
    }
}
