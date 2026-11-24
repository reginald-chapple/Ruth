using Microsoft.EntityFrameworkCore;
using Ruth.Persistence.EntityFramework;
using Ruth.Persistence.Scheduling.Configurations;

namespace Ruth.Persistence.Scheduling;

/// <summary>Dedicated EF Core <see cref="DbContext" /> for the scheduler schema.</summary>
public sealed class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options)
    : DbContext(options)
{
    /// <summary>Gets the PostgreSQL schema name for scheduler tables.</summary>
    public const string SchemaName = "scheduler";

    /// <summary>Gets the registered scheduled jobs table.</summary>
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();

    /// <summary>Gets the scheduled job execution histories table.</summary>
    public DbSet<ScheduledJobHistory> ScheduledJobHistories => Set<ScheduledJobHistory>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        _ = modelBuilder.HasDefaultSchema(SchemaName);
        _ = modelBuilder.ApplyConfiguration(new ScheduledJobConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ScheduledJobHistoryConfiguration());
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        RuthModelBuilderConventions.ConfigureConventions(
            configurationBuilder,
            additionalIdAssembly: null
        );
    }
}
