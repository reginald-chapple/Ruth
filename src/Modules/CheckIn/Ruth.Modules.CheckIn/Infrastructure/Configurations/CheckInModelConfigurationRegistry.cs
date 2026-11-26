using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal static class CheckInModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new CheckInConfigurationEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CheckInAreaEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CheckInAreaLocationEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AttendanceCheckInSessionEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CheckInAttendanceEntryEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DailySecurityPickupCodeEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CheckInLabelEntityConfiguration());
    }
}
