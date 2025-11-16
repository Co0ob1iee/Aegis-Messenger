using Aegis.Modules.Security.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Modules.Security.Infrastructure.Persistence.Configurations;

public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.UserId);

        builder.Property(log => log.EventType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(log => log.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.IpAddress)
            .HasMaxLength(45);  // IPv6 max length

        builder.Property(log => log.UserAgent)
            .HasMaxLength(500);

        builder.Property(log => log.Timestamp)
            .IsRequired();

        builder.Property(log => log.Details)
            .HasMaxLength(2000);

        builder.Property(log => log.IsSuccessful)
            .IsRequired();

        builder.Property(log => log.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(log => log.RelatedEntityId);

        builder.Property(log => log.RelatedEntityType)
            .HasMaxLength(100);

        // Indexes for performance
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.EventType);
        builder.HasIndex(log => log.Timestamp);
        builder.HasIndex(log => log.Severity);
        builder.HasIndex(log => log.IsSuccessful);
        builder.HasIndex(log => new { log.IpAddress, log.Timestamp });
        builder.HasIndex(log => new { log.UserId, log.EventType, log.Timestamp });

        // Ignore navigation properties
        builder.Ignore(log => log.DomainEvents);
    }
}
