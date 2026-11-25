using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Scheduling;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupMemberAssignmentConfiguration
    : IEntityTypeConfiguration<GroupMemberAssignment>
{
    public void Configure(EntityTypeBuilder<GroupMemberAssignment> builder)
    {
        _ = builder.ToTable("group_member_assignments");
        _ = builder.HasKey(static a => a.Id);
        _ = builder.Property(static a => a.Id).ValueGeneratedNever();
        _ = builder.HasIndex(static a => a.GroupId);
        _ = builder
            .HasIndex(static a => new
            {
                a.GroupMemberId,
                a.LocationId,
                a.ScheduleId,
            })
            .AreNullsDistinct(nullsDistinct: false)
            .IsUnique();
    }
}
