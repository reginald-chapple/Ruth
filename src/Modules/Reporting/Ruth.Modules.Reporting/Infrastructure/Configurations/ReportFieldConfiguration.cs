using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.Reports;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class ReportFieldConfiguration : IEntityTypeConfiguration<ReportField>
{
    public void Configure(EntityTypeBuilder<ReportField> builder)
    {
        _ = builder.ToTable("report_fields");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.FieldSelectKey).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.ColumnHeaderText).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.SelectionConfigJson).HasColumnType("jsonb");
        _ = builder.Property(static x => x.SortDirection).HasConversion<string>().HasMaxLength(24);
        _ = builder.HasIndex(static x => new { x.ReportId, x.ColumnOrder });
    }
}
