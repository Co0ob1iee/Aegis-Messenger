using Aegis.Modules.Users.Domain.Entities;
using Aegis.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Modules.Users.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DisplayName).HasMaxLength(100);
        builder.Property(p => p.Bio).HasMaxLength(500);
        builder.Property(p => p.AvatarUrl).HasMaxLength(500);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property<List<Guid>>("_contactIds")
            .HasColumnName("ContactIds")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions)null!) ?? new List<Guid>());

        builder.Property<List<Guid>>("_blockedUserIds")
            .HasColumnName("BlockedUserIds")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions)null!) ?? new List<Guid>());

        // Privacy Settings - Owned Entity (stored as JSON)
        builder.OwnsOne(p => p.PrivacySettings, privacy =>
        {
            privacy.Property(ps => ps.OnlineStatusVisibility).HasConversion<string>().HasMaxLength(20);
            privacy.Property(ps => ps.LastSeenVisibility).HasConversion<string>().HasMaxLength(20);
            privacy.Property(ps => ps.ProfilePictureVisibility).HasConversion<string>().HasMaxLength(20);
            privacy.Property(ps => ps.BioVisibility).HasConversion<string>().HasMaxLength(20);
            privacy.Property(ps => ps.SendReadReceipts);
            privacy.Property(ps => ps.SendDeliveryReceipts);
            privacy.Property(ps => ps.SendTypingIndicators);
            privacy.Property(ps => ps.DefaultMessageExpiration);
            privacy.Property(ps => ps.FuzzTimestamps);
            privacy.Property(ps => ps.UseMessagePadding);
        });

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.ContactIds);
        builder.Ignore(p => p.BlockedUserIds);
    }
}
