using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Scheduling;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class PersonScheduleExclusionConfiguration
    : IEntityTypeConfiguration<PersonScheduleExclusion>
{
    public void Configure(EntityTypeBuilder<PersonScheduleExclusion> builder)
    {
        _ = builder.ToTable("person_schedule_exclusions");
        _ = builder.HasKey(static e => e.Id);
        _ = builder.Property(static e => e.Id).ValueGeneratedNever();
        _ = builder.Property(static e => e.Title).HasMaxLength(100);
        _ = builder.HasIndex(static e => new
        {
            e.PersonId,
            e.StartDate,
            e.EndDate,
        });
        _ = builder.HasIndex(static e => e.GroupId);
    }
}
