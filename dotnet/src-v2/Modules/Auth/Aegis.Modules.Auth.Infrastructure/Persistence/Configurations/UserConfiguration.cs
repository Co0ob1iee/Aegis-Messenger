using Aegis.Modules.Auth.Domain.Entities;
using Aegis.Modules.Auth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Modules.Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for User aggregate
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // Username value object
        builder.OwnsOne(u => u.Username, username =>
        {
            username.Property(un => un.Value)
                .HasColumnName("Username")
                .HasMaxLength(50)
                .IsRequired();

            username.HasIndex(un => un.Value)
                .IsUnique();
        });

        // Email value object
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(255)
                .IsRequired();

            email.HasIndex(e => e.Value)
                .IsUnique();
        });

        // HashedPassword value object
        builder.OwnsOne(u => u.Password, password =>
        {
            password.Property(p => p.Hash)
                .HasColumnName("PasswordHash")
                .HasMaxLength(255)
                .IsRequired();

            password.Property(p => p.Salt)
                .HasColumnName("PasswordSalt")
                .HasMaxLength(255)
                .IsRequired();
        });

        // PhoneNumber value object (optional)
        builder.OwnsOne(u => u.PhoneNumber, phoneNumber =>
        {
            phoneNumber.Property(pn => pn.Value)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(20);
        });

        // Refresh tokens collection
        builder.OwnsMany(u => u.RefreshTokens, refreshToken =>
        {
            refreshToken.ToTable("RefreshTokens");

            refreshToken.WithOwner()
                .HasForeignKey("UserId");

            refreshToken.Property<int>("Id");
            refreshToken.HasKey("Id");

            refreshToken.Property(rt => rt.Token)
                .HasMaxLength(500)
                .IsRequired();

            refreshToken.Property(rt => rt.ExpiresAt)
                .IsRequired();

            refreshToken.Property(rt => rt.CreatedAt)
                .IsRequired();

            refreshToken.Property(rt => rt.IsRevoked)
                .IsRequired();

            refreshToken.Property(rt => rt.RevokedAt);

            refreshToken.HasIndex(rt => rt.Token);
        });

        // Regular properties
        builder.Property(u => u.IsEmailVerified)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLoginAt);

        // Ignore domain events (not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}
