using Aegis.Modules.Messages.Domain.Entities;
using Aegis.Modules.Messages.Domain.Enums;
using Aegis.Modules.Messages.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Modules.Messages.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for Message aggregate
/// </summary>
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .IsRequired();

        builder.Property(m => m.RecipientId)
            .IsRequired();

        // EncryptedContent value object
        builder.OwnsOne(m => m.Content, content =>
        {
            content.Property(c => c.Ciphertext)
                .HasColumnName("EncryptedContent")
                .IsRequired();

            content.Property(c => c.IsPreKeyMessage)
                .HasColumnName("IsPreKeyMessage")
                .IsRequired();
        });

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.SentAt)
            .IsRequired();

        builder.Property(m => m.DeliveredAt);

        builder.Property(m => m.ReadAt);

        builder.Property(m => m.ReplyToMessageId);

        builder.Property(m => m.IsGroupMessage)
            .IsRequired();

        builder.Property(m => m.GroupId);

        builder.Property(m => m.IsDeleted)
            .IsRequired();

        // Indexes
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.RecipientId);
        builder.HasIndex(m => new { m.RecipientId, m.Status });
        builder.HasIndex(m => m.SentAt);

        // Ignore domain events
        builder.Ignore(m => m.DomainEvents);
    }
}
