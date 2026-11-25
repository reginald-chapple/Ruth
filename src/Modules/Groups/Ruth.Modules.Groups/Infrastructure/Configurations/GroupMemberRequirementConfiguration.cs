using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Requirements;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupMemberRequirementConfiguration
    : IEntityTypeConfiguration<GroupMemberRequirement>
{
    public void Configure(EntityTypeBuilder<GroupMemberRequirement> builder)
    {
        _ = builder.ToTable("group_member_requirements");
        _ = builder.HasKey(static mr => mr.Id);
        _ = builder.Property(static mr => mr.Id).ValueGeneratedNever();
        _ = builder.Property(static mr => mr.State).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static mr => mr.OverrideNote).HasMaxLength(2000);
        _ = builder
            .HasIndex(static mr => new { mr.GroupMemberId, mr.GroupRequirementId })
            .IsUnique();
    }
}
