using Aegis.Modules.Files.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Files.Infrastructure.Persistence;

public class FilesDbContext : DbContext
{
    public FilesDbContext(DbContextOptions<FilesDbContext> options) : base(options) { }

    public DbSet<FileMetadata> Files => Set<FileMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("files");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
