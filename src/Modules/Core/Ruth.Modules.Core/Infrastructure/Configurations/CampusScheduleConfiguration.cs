using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class CampusScheduleConfiguration : IEntityTypeConfiguration<CampusSchedule>
{
    public void Configure(EntityTypeBuilder<CampusSchedule> builder)
    {
        _ = builder.ToTable("campus_schedules");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.CampusId).HasColumnName("campus_id").IsRequired();
        _ = builder.Property(x => x.ScheduleId).HasColumnName("schedule_id").IsRequired();
        _ = builder.Property(x => x.ScheduleTypeValueId).HasColumnName("schedule_type_value_id");
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();

        _ = builder.HasIndex(x => new { x.CampusId, x.Order });
    }
}
