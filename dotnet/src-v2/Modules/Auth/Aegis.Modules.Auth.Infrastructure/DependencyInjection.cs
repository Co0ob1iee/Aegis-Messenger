using Aegis.Modules.Auth.Application.Abstractions;
using Aegis.Modules.Auth.Domain.Repositories;
using Aegis.Modules.Auth.Infrastructure.Persistence;
using Aegis.Modules.Auth.Infrastructure.Persistence.Repositories;
using Aegis.Modules.Auth.Infrastructure.Services;
using Aegis.Shared.Cryptography.Encryption;
using Aegis.Shared.Cryptography.Interfaces;
using Aegis.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Auth.Infrastructure;

/// <summary>
/// Dependency injection configuration for Auth Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AuthDatabase") ??
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "auth")));

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp =>
        {
            var context = sp.GetRequiredService<AuthDbContext>();
            return new UnitOfWork(context);
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IKeyDerivation, KeyDerivationService>();

        return services;
    }
}
