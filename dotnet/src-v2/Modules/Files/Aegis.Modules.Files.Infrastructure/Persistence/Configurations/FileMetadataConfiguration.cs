using Aegis.Modules.Files.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Modules.Files.Infrastructure.Persistence.Configurations;

public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.ToTable("FileMetadata");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName).HasMaxLength(255).IsRequired();
        builder.Property(f => f.FileSize).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(f => f.EncryptedPath).HasMaxLength(500).IsRequired();
        builder.Property(f => f.EncryptionKey).IsRequired();
        builder.Property(f => f.UploadedBy).IsRequired();
        builder.Property(f => f.UploadedAt).IsRequired();
        builder.Property(f => f.IsDeleted).IsRequired();

        builder.HasIndex(f => f.UploadedBy);
        builder.HasIndex(f => f.UploadedAt);

        builder.Ignore(f => f.DomainEvents);
    }
}
