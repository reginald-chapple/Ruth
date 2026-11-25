using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Attendances;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        _ = builder.ToTable("attendances");
        _ = builder.HasKey(static a => a.Id);
        _ = builder.Property(static a => a.Id).ValueGeneratedNever();
        _ = builder.Property(static a => a.Rsvp).HasConversion<string>().HasMaxLength(16);
        _ = builder.Property(static a => a.RsvpToken).HasMaxLength(64);
        _ = builder.Property(static a => a.Note).HasMaxLength(2000);
        _ = builder.Property(static a => a.DeclineReasonNote).HasMaxLength(2000);

        _ = builder.HasIndex(static a => a.OccurrenceId);
        _ = builder.HasIndex(static a => a.PersonId);
        _ = builder.HasIndex(static a => a.BranchId);
        _ = builder.HasIndex(static a => a.StartDateTimeUtc);
        _ = builder.HasIndex(static a => a.RsvpToken).IsUnique();
    }
}
