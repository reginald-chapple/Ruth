using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Domain.Sessions;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class AttendanceCheckInSessionEntityConfiguration
    : IEntityTypeConfiguration<AttendanceCheckInSession>
{
    public void Configure(EntityTypeBuilder<AttendanceCheckInSession> builder)
    {
        _ = builder.ToTable("attendance_check_in_sessions");
        _ = builder.HasKey(static s => s.Id);
        _ = builder.Property(static s => s.Id).ValueGeneratedNever();

        _ = builder.Property(static s => s.ConfigurationName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static s => s.Channel).HasConversion<string>().HasMaxLength(24);
        _ = builder.Property(static s => s.SearchModeUsed).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static s => s.SearchTermSummary).HasMaxLength(120);
        _ = builder.Property(static s => s.SecurityPickupCode).HasMaxLength(16);
        _ = builder.Property(static s => s.MobileExpressToken).HasMaxLength(24);
        _ = builder.Property(static s => s.ClientIpAddress).HasMaxLength(64);

        _ = builder.Ignore(static s => s.IntegrationEvents);

        _ = builder
            .HasMany(static s => s.Entries)
            .WithOne()
            .HasForeignKey(static e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static s => s.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static s => new { s.CheckInConfigurationId, s.ServiceDate });
        _ = builder.HasIndex(static s => new { s.ServiceDate, s.SecurityPickupCode });
        _ = builder.HasIndex(static s => s.MobileExpressToken);
        _ = builder.HasIndex(static s => s.HouseholdId);
    }
}
