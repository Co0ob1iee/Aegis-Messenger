using Aegis.Modules.Files.Application;
using Aegis.Modules.Files.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Files.API;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFilesApplication();
        services.AddFilesInfrastructure(configuration);

        return services;
    }
}
