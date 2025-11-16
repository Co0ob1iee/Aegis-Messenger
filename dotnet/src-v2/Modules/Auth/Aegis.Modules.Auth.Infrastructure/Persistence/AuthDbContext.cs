using Aegis.Modules.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Auth.Infrastructure.Persistence;

/// <summary>
/// Database context for Auth module
/// Manages User aggregate and related entities
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
