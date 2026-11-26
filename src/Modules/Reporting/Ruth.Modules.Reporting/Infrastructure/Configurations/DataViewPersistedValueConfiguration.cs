using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.DataViews;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class DataViewPersistedValueConfiguration
    : IEntityTypeConfiguration<DataViewPersistedValue>
{
    public void Configure(EntityTypeBuilder<DataViewPersistedValue> builder)
    {
        _ = builder.ToTable("data_view_persisted_values");
        _ = builder.HasKey(static x => new { x.DataViewId, x.EntityId });
        _ = builder.HasIndex(static x => x.EntityId);
    }
}
