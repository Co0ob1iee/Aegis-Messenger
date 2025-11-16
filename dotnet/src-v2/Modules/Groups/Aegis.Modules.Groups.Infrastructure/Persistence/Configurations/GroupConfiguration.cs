using Aegis.Modules.Groups.Domain.Entities;
using Aegis.Modules.Groups.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Modules.Groups.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(500);
        builder.Property(g => g.AvatarUrl).HasMaxLength(500);

        builder.OwnsMany(g => g.Members, members =>
        {
            members.ToTable("GroupMembers");
            members.WithOwner().HasForeignKey("GroupId");
            members.Property<int>("Id");
            members.HasKey("Id");

            members.Property(m => m.UserId).IsRequired();
            members.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            members.Property(m => m.JoinedAt).IsRequired();
        });

        builder.Ignore(g => g.DomainEvents);
    }
}
