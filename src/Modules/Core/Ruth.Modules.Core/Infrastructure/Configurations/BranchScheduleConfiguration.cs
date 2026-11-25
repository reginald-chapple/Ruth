using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class BranchScheduleConfiguration : IEntityTypeConfiguration<BranchSchedule>
{
    public void Configure(EntityTypeBuilder<BranchSchedule> builder)
    {
        _ = builder.ToTable("branch_schedules");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        _ = builder.Property(x => x.ScheduleId).HasColumnName("schedule_id").IsRequired();
        _ = builder.Property(x => x.ScheduleTypeValueId).HasColumnName("schedule_type_value_id");
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();

        _ = builder.HasIndex(x => new { x.BranchId, x.Order });
    }
}
