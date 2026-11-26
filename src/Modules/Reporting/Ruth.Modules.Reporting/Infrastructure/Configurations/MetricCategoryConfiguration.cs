using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.Metrics;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class MetricCategoryConfiguration : IEntityTypeConfiguration<MetricCategory>
{
    public void Configure(EntityTypeBuilder<MetricCategory> builder)
    {
        _ = builder.ToTable("metric_categories");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Ignore(static x => x.DomainEvents);
    }
}
