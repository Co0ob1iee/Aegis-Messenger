using Aegis.Modules.Security.Domain.Repositories;
using Aegis.Modules.Security.Infrastructure.Persistence;
using Aegis.Modules.Security.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Security.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<SecurityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SecurityDatabase") ??
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "security")));

        // Repositories
        services.AddScoped<ISecurityAuditRepository, SecurityAuditRepository>();

        return services;
    }
}
