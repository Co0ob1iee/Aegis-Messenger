using Aegis.Modules.Users.Domain.Repositories;
using Aegis.Modules.Users.Infrastructure.Persistence;
using Aegis.Modules.Users.Infrastructure.Persistence.Repositories;
using Aegis.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("UsersDatabase") ??
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "users")));

        services.AddScoped<IUnitOfWork>(sp =>
        {
            var context = sp.GetRequiredService<UsersDbContext>();
            return new UnitOfWork(context);
        });

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        return services;
    }
}
