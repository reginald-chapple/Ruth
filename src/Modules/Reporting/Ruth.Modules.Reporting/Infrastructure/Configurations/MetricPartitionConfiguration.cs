using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.Metrics;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class MetricPartitionConfiguration : IEntityTypeConfiguration<MetricPartition>
{
    public void Configure(EntityTypeBuilder<MetricPartition> builder)
    {
        _ = builder.ToTable("metric_partitions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Label).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.EntityTypeKey).HasMaxLength(160);
        _ = builder.HasIndex(static x => new { x.MetricId, x.Order });
    }
}
