using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Scheduling;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupScheduleExclusionConfiguration
    : IEntityTypeConfiguration<GroupScheduleExclusion>
{
    public void Configure(EntityTypeBuilder<GroupScheduleExclusion> builder)
    {
        _ = builder.ToTable("group_schedule_exclusions");
        _ = builder.HasKey(static e => e.Id);
        _ = builder.Property(static e => e.Id).ValueGeneratedNever();
        _ = builder.Property(static e => e.Title).HasMaxLength(100);
        _ = builder.HasIndex(static e => new
        {
            e.GroupTypeId,
            e.StartDate,
            e.EndDate,
        });
    }
}
