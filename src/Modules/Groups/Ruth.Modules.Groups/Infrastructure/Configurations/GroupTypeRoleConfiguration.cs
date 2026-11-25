using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.GroupTypes;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupTypeRoleConfiguration : IEntityTypeConfiguration<GroupTypeRole>
{
    public void Configure(EntityTypeBuilder<GroupTypeRole> builder)
    {
        _ = builder.ToTable("group_type_roles");
        _ = builder.HasKey(static r => r.Id);
        _ = builder.Property(static r => r.Id).ValueGeneratedNever();
        _ = builder.Property(static r => r.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static r => r.Description).HasMaxLength(2000);
        _ = builder.HasIndex(static r => new { r.GroupTypeId, r.Name }).IsUnique();
    }
}
