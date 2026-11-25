using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Requirements;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupRequirementConfiguration : IEntityTypeConfiguration<GroupRequirement>
{
    public void Configure(EntityTypeBuilder<GroupRequirement> builder)
    {
        _ = builder.ToTable("group_requirements");
        _ = builder.HasKey(static r => r.Id);
        _ = builder.Property(static r => r.Id).ValueGeneratedNever();
        _ = builder
            .Property(static r => r.AppliesToAgeClassification)
            .HasConversion<string>()
            .HasMaxLength(16);
        _ = builder.Property(static r => r.DueDateAttributeKey).HasMaxLength(150);
        _ = builder.HasIndex(static r => r.GroupId);
        _ = builder.HasIndex(static r => r.GroupTypeId);
        _ = builder
            .HasIndex(static r => new
            {
                r.GroupId,
                r.GroupTypeId,
                r.GroupRequirementTypeId,
                r.GroupRoleId,
            })
            .AreNullsDistinct(nullsDistinct: false)
            .IsUnique();
    }
}
