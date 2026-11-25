using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal static class CheckInModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new CheckInConfigurationEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CheckInAreaEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CheckInAreaLocationEntityConfiguration());
    }
}
