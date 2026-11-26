using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.Metrics;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class MetricValuePartitionConfiguration
    : IEntityTypeConfiguration<MetricValuePartition>
{
    public void Configure(EntityTypeBuilder<MetricValuePartition> builder)
    {
        _ = builder.ToTable("metric_value_partitions");
        _ = builder.HasKey(static x => new { x.MetricValueId, x.MetricPartitionId });
        _ = builder.Property(static x => x.EntityLabel).HasMaxLength(160);
        _ = builder.HasIndex(static x => x.EntityId);
    }
}
