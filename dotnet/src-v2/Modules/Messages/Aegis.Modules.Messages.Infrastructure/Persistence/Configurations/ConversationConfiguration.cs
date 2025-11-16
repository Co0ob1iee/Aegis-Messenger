using Aegis.Modules.Messages.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Modules.Messages.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for Conversation aggregate
/// </summary>
public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.IsGroup)
            .IsRequired();

        builder.Property(c => c.GroupId);

        builder.Property(c => c.CreatedBy)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.LastMessageAt);

        builder.Property(c => c.LastMessageId);

        // Participant IDs as JSON array
        builder.Property<List<Guid>>("_participantIds")
            .HasColumnName("ParticipantIds")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions)null!) ?? new List<Guid>())
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.IsGroup);
        builder.HasIndex(c => c.GroupId);
        builder.HasIndex(c => c.LastMessageAt);

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.ParticipantIds);
    }
}
