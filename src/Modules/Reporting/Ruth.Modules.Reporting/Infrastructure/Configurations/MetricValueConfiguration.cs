using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.Metrics;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class MetricValueConfiguration : IEntityTypeConfiguration<MetricValue>
{
    public void Configure(EntityTypeBuilder<MetricValue> builder)
    {
        _ = builder.ToTable("metric_values");
        _ = builder.HasKey(static x => x.Id);
        _ = builder
            .Property(static x => x.MetricValueType)
            .HasConversion<string>()
            .HasMaxLength(16);
        _ = builder.Property(static x => x.YValue).HasPrecision(18, 4);
        _ = builder.Property(static x => x.XValue).HasMaxLength(120);
        _ = builder.Property(static x => x.Note).HasMaxLength(1000);
        _ = builder
            .HasMany(static x => x.Partitions)
            .WithOne()
            .HasForeignKey(static p => p.MetricValueId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Partitions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.MetricId, x.MetricValueDateTimeUtc });
    }
}
