using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.Metrics;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class MetricConfiguration : IEntityTypeConfiguration<Metric>
{
    public void Configure(EntityTypeBuilder<Metric> builder)
    {
        _ = builder.ToTable("metrics");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Title).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.Subtitle).HasMaxLength(240);
        _ = builder.Property(static x => x.MetricKey).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.SourceType).HasConversion<string>().HasMaxLength(24);
        _ = builder.Property(static x => x.SourceQueryKey).HasMaxLength(120);
        _ = builder.Property(static x => x.ScheduleCronExpression).HasMaxLength(80);
        _ = builder.Property(static x => x.YAxisLabel).HasMaxLength(80);
        _ = builder.Property(static x => x.UnitLabel).HasMaxLength(40);
        _ = builder
            .HasMany(static x => x.Partitions)
            .WithOne()
            .HasForeignKey(static p => p.MetricId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Partitions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.MetricKey).IsUnique();
    }
}
