using Aegis.Modules.Groups.Domain.Repositories;
using Aegis.Modules.Groups.Infrastructure.Persistence;
using Aegis.Modules.Groups.Infrastructure.Persistence.Repositories;
using Aegis.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Modules.Groups.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGroupsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<GroupsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("GroupsDatabase") ??
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "groups")));

        services.AddScoped<IUnitOfWork>(sp =>
        {
            var context = sp.GetRequiredService<GroupsDbContext>();
            return new UnitOfWork(context);
        });

        services.AddScoped<IGroupRepository, GroupRepository>();

        return services;
    }
}
