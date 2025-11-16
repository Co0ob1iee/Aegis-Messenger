using Aegis.Modules.Messages.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Messages.Infrastructure.Persistence;

/// <summary>
/// Database context for Messages module
/// Manages Message and Conversation aggregates
/// </summary>
public class MessagesDbContext : DbContext
{
    public MessagesDbContext(DbContextOptions<MessagesDbContext> options) : base(options)
    {
    }

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Conversation> Conversations => Set<Conversation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("messages");

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessagesDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
