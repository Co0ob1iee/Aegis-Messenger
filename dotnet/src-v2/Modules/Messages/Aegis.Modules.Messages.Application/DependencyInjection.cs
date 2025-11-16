using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Messages.Application;

/// <summary>
/// Dependency injection configuration for Messages Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMessagesApplication(this IServiceCollection services)
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
