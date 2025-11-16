using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Files.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        return services;
    }
}
