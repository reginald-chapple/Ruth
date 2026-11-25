using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Attendances;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class AttendanceOccurrenceConfiguration
    : IEntityTypeConfiguration<AttendanceOccurrence>
{
    public void Configure(EntityTypeBuilder<AttendanceOccurrence> builder)
    {
        _ = builder.ToTable("attendance_occurrences");
        _ = builder.HasKey(static o => o.Id);
        _ = builder.Property(static o => o.Id).ValueGeneratedNever();
        _ = builder.Property(static o => o.Name).HasMaxLength(150);
        _ = builder.Property(static o => o.Notes).HasMaxLength(2000);
        _ = builder.Property(static o => o.AcceptConfirmationMessage).HasMaxLength(2000);
        _ = builder.Property(static o => o.DeclineConfirmationMessage).HasMaxLength(2000);

        _ = builder
            .HasMany(static o => o.Attendances)
            .WithOne()
            .HasForeignKey(static a => a.OccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static o => o.Attendances)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static o => o.OccurrenceDate);
        _ = builder.HasIndex(static o => o.SundayDate);
        _ = builder.HasIndex(static o => o.OccurrenceDateKey);
        _ = builder
            .HasIndex(static o => new
            {
                o.GroupId,
                o.LocationId,
                o.ScheduleId,
                o.OccurrenceDate,
            })
            .AreNullsDistinct(nullsDistinct: false)
            .IsUnique();
    }
}
