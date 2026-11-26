using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Domain.Sessions;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class CheckInAttendanceEntryEntityConfiguration
    : IEntityTypeConfiguration<CheckInAttendanceEntry>
{
    public void Configure(EntityTypeBuilder<CheckInAttendanceEntry> builder)
    {
        _ = builder.ToTable("check_in_attendance_entries");
        _ = builder.HasKey(static e => e.Id);
        _ = builder.Property(static e => e.Id).ValueGeneratedNever();

        _ = builder.Property(static e => e.AreaName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static e => e.PersonFullName).HasMaxLength(160).IsRequired();
        _ = builder.Property(static e => e.LocationName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static e => e.Status).HasConversion<string>().HasMaxLength(24);
        _ = builder.Property(static e => e.SecurityPickupCode).HasMaxLength(16);
        _ = builder.Property(static e => e.OverrideReason).HasMaxLength(500);
        _ = builder.Property(static e => e.AttendanceNote).HasMaxLength(500);
        _ = builder.Property(static e => e.VerifiedPickupCode).HasMaxLength(16);

        _ = builder.HasIndex(static e => new
        {
            e.CheckInConfigurationId,
            e.ServiceDate,
            e.Status,
        });
        _ = builder.HasIndex(static e => new
        {
            e.PersonId,
            e.ServiceDate,
            e.ScheduleId,
        });
        _ = builder.HasIndex(static e => new
        {
            e.LocationId,
            e.ServiceDate,
            e.Status,
        });
        _ = builder.HasIndex(static e => new { e.ServiceDate, e.SecurityPickupCode });
    }
}
