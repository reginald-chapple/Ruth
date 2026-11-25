using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Schedules;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class ScheduleDateConfiguration : IEntityTypeConfiguration<ScheduleDate>
{
    public void Configure(EntityTypeBuilder<ScheduleDate> builder)
    {
        _ = builder.ToTable("schedule_dates");
        _ = builder.HasKey(x => new { x.ScheduleId, x.StartOnUtc });

        _ = builder.Property(x => x.ScheduleId).HasColumnName("schedule_id").IsRequired();
        _ = builder.Property(x => x.StartOnUtc).HasColumnName("start_on_utc").IsRequired();
        _ = builder.Property(x => x.EndOnUtc).HasColumnName("end_on_utc").IsRequired();
        _ = builder.Property(x => x.StartDateKey).HasColumnName("start_date_key").IsRequired();

        _ = builder.HasIndex(x => x.StartDateKey);
        _ = builder.HasIndex(x => new { x.StartOnUtc, x.EndOnUtc });
    }
}
