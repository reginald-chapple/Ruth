using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Recurring;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class RecurringScheduleSplitEntityConfiguration
    : IEntityTypeConfiguration<RecurringScheduleSplit>
{
    public void Configure(EntityTypeBuilder<RecurringScheduleSplit> builder)
    {
        _ = builder.ToTable("recurring_schedule_splits");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.ScheduleId).IsRequired();
        _ = builder.Property(static x => x.AccountId).IsRequired();
        _ = builder.Property(static x => x.Amount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(300);

        _ = builder.HasIndex(static x => x.ScheduleId);
    }
}
