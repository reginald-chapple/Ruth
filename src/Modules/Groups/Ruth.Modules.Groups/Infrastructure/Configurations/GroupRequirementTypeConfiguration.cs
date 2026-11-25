using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Requirements;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupRequirementTypeConfiguration
    : IEntityTypeConfiguration<GroupRequirementType>
{
    public void Configure(EntityTypeBuilder<GroupRequirementType> builder)
    {
        _ = builder.ToTable("group_requirement_types");
        _ = builder.HasKey(static t => t.Id);
        _ = builder.Property(static t => t.Id).ValueGeneratedNever();
        _ = builder.Property(static t => t.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static t => t.Description).HasMaxLength(2000);
        _ = builder.Property(static t => t.Summary).HasMaxLength(2000);
        _ = builder.Property(static t => t.CheckType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static t => t.DueDateType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static t => t.RuleKey).HasMaxLength(150);
        _ = builder.Property(static t => t.RuleValue).HasMaxLength(500);
        _ = builder.Property(static t => t.PositiveLabel).HasMaxLength(150);
        _ = builder.Property(static t => t.NegativeLabel).HasMaxLength(150);
        _ = builder.Property(static t => t.WarningLabel).HasMaxLength(150);
        _ = builder.Property(static t => t.CheckboxLabel).HasMaxLength(150);
        _ = builder.Property(static t => t.IconCssClass).HasMaxLength(100);
        _ = builder.HasIndex(static t => t.Name).IsUnique();
    }
}
