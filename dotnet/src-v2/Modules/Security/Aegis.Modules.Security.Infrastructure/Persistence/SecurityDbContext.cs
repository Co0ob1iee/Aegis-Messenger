using Aegis.Modules.Security.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Security.Infrastructure.Persistence;

/// <summary>
/// Database context for Security module
/// </summary>
public class SecurityDbContext : DbContext
{
    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;

    public SecurityDbContext(DbContextOptions<SecurityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("security");

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SecurityDbContext).Assembly);
    }
}
