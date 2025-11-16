using System;
using Aegis.Core.Models;
using Aegis.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Data.Context;

/// <summary>
/// Main database context for Aegis Messenger
/// Uses SQL Server with encryption
/// </summary>
public class AegisDbContext : DbContext
{
    public AegisDbContext(DbContextOptions<AegisDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<MessageEntity> Messages { get; set; } = null!;
    public DbSet<GroupEntity> Groups { get; set; } = null!;
    public DbSet<GroupMemberEntity> GroupMembers { get; set; } = null!;
    public DbSet<ContactEntity> Contacts { get; set; } = null!;
    public DbSet<PreKeyBundleEntity> PreKeyBundles { get; set; } = null!;
    public DbSet<FileAttachmentEntity> FileAttachments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.PhoneNumber);

            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.PasswordSalt).IsRequired();

            // Privacy settings as JSON
            entity.OwnsOne(e => e.PrivacySettings);
        });

        // Message configuration
        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SenderId, e.ReceiverId });
            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.EncryptedContent).IsRequired();

            // Foreign keys
            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(e => e.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Group configuration
        modelBuilder.Entity<GroupEntity>(entity =>
        {
            entity.ToTable("Groups");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatorId);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

            entity.HasMany(e => e.Members)
                .WithOne()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Group settings as JSON
            entity.OwnsOne(e => e.Settings);
        });

        // GroupMember configuration
        modelBuilder.Entity<GroupMemberEntity>(entity =>
        {
            entity.ToTable("GroupMembers");
            entity.HasKey(e => new { e.GroupId, e.UserId });

            entity.HasIndex(e => e.UserId);
        });

        // Contact configuration
        modelBuilder.Entity<ContactEntity>(entity =>
        {
            entity.ToTable("Contacts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OwnerId, e.ContactUserId }).IsUnique();

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(e => e.ContactUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PreKeyBundle configuration
        modelBuilder.Entity<PreKeyBundleEntity>(entity =>
        {
            entity.ToTable("PreKeyBundles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.DeviceId });

            entity.Property(e => e.IdentityKey).IsRequired();
            entity.Property(e => e.PreKeyPublic).IsRequired();
            entity.Property(e => e.SignedPreKeyPublic).IsRequired();
        });

        // FileAttachment configuration
        modelBuilder.Entity<FileAttachmentEntity>(entity =>
        {
            entity.ToTable("FileAttachments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UploaderId);

            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
        });
    }
}
