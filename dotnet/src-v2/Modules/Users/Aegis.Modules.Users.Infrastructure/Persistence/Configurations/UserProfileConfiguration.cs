using Aegis.Modules.Users.Domain.Entities;
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

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.ContactIds);
        builder.Ignore(p => p.BlockedUserIds);
    }
}
