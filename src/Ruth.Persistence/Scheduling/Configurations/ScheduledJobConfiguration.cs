using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ruth.Persistence.Scheduling.Configurations;

internal sealed class ScheduledJobConfiguration : IEntityTypeConfiguration<ScheduledJob>
{
    public void Configure(EntityTypeBuilder<ScheduledJob> builder)
    {
        _ = builder.ToTable("scheduled_jobs");

        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        _ = builder.Property(x => x.JobKey).HasColumnName("job_key").HasMaxLength(200).IsRequired();
        _ = builder.HasIndex(x => x.JobKey).IsUnique();

        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        _ = builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        _ = builder
            .Property(x => x.CronExpression)
            .HasColumnName("cron_expression")
            .HasMaxLength(120)
            .IsRequired();

        _ = builder
            .Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(value: true)
            .IsRequired();
        _ = builder
            .Property(x => x.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(value: false)
            .IsRequired();
        _ = builder.Property(x => x.LastRunUtc).HasColumnName("last_run_utc");
        _ = builder.Property(x => x.LastSuccessfulRunUtc).HasColumnName("last_successful_run_utc");
        _ = builder.Property(x => x.LastDurationMs).HasColumnName("last_duration_ms");
        _ = builder.Property(x => x.LastStatus).HasColumnName("last_status").HasMaxLength(32);
        _ = builder.Property(x => x.LastStatusMessage).HasColumnName("last_status_message");
        _ = builder
            .Property(x => x.LastRunWorkerId)
            .HasColumnName("last_run_worker_id")
            .HasMaxLength(128);
        _ = builder
            .Property(x => x.HistoryCount)
            .HasColumnName("history_count")
            .HasDefaultValue(500)
            .IsRequired();
        _ = builder.Property(x => x.NextRunUtc).HasColumnName("next_run_utc");

        _ = builder.Property(x => x.Version).IsRowVersion();

        _ = builder.HasIndex(x => new { x.IsActive, x.NextRunUtc });
    }
}
