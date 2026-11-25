using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Scheduling;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupLocationScheduleConfigConfiguration
    : IEntityTypeConfiguration<GroupLocationScheduleConfig>
{
    public void Configure(EntityTypeBuilder<GroupLocationScheduleConfig> builder)
    {
        _ = builder.ToTable("group_location_schedule_configs");
        _ = builder.HasKey(static c => c.Id);
        _ = builder.Property(static c => c.Id).ValueGeneratedNever();
        _ = builder.Property(static c => c.ConfigurationName).HasMaxLength(100);
        _ = builder.Property(static c => c.ConfirmationAdditionalDetails).HasMaxLength(2000);
        _ = builder.Property(static c => c.ReminderAdditionalDetails).HasMaxLength(2000);
        _ = builder.HasIndex(static c => new { c.GroupLocationId, c.ScheduleId }).IsUnique();
    }
}
