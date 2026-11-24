using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ruth.Persistence.Scheduling.Configurations;

internal sealed class ScheduledJobHistoryConfiguration
    : IEntityTypeConfiguration<ScheduledJobHistory>
{
    public void Configure(EntityTypeBuilder<ScheduledJobHistory> builder)
    {
        _ = builder.ToTable("scheduled_job_histories");

        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        _ = builder.Property(x => x.ScheduledJobId).HasColumnName("scheduled_job_id").IsRequired();
        _ = builder
            .Property(x => x.WorkerId)
            .HasColumnName("worker_id")
            .HasMaxLength(128)
            .IsRequired();
        _ = builder.Property(x => x.StartedOnUtc).HasColumnName("started_on_utc").IsRequired();
        _ = builder.Property(x => x.CompletedOnUtc).HasColumnName("completed_on_utc");
        _ = builder.Property(x => x.DurationMs).HasColumnName("duration_ms");
        _ = builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        _ = builder.Property(x => x.StatusMessage).HasColumnName("status_message");

        _ = builder
            .HasOne<ScheduledJob>()
            .WithMany()
            .HasForeignKey(x => x.ScheduledJobId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasIndex(x => new { x.ScheduledJobId, x.StartedOnUtc });
    }
}
