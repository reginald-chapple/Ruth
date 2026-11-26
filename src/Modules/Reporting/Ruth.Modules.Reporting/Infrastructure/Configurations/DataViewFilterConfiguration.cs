using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.DataViews;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class DataViewFilterConfiguration : IEntityTypeConfiguration<DataViewFilter>
{
    public void Configure(EntityTypeBuilder<DataViewFilter> builder)
    {
        _ = builder.ToTable("data_view_filters");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.ExpressionType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.RuleKind).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.FieldKey).HasMaxLength(160);
        _ = builder.Property(static x => x.Operator).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.Value).HasMaxLength(2000);
        _ = builder.Property(static x => x.SecondaryValue).HasMaxLength(2000);
        _ = builder.HasIndex(static x => new
        {
            x.DataViewId,
            x.ParentFilterId,
            x.Order,
        });
    }
}
