using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Auth.Application;

/// <summary>
/// Dependency injection configuration for Auth Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
