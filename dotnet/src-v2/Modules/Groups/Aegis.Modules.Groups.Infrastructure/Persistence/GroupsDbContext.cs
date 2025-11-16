using Aegis.Modules.Groups.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Groups.Infrastructure.Persistence;

public class GroupsDbContext : DbContext
{
    public GroupsDbContext(DbContextOptions<GroupsDbContext> options) : base(options) { }

    public DbSet<Group> Groups => Set<Group>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("groups");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GroupsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
