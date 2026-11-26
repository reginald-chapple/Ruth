using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Domain.Analytics;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal static class ReportingStarSchemaConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<DimDate>(static b =>
        {
            _ = b.ToTable("dim_date");
            _ = b.HasKey(static x => new { x.TenantId, x.DateKey });
            _ = b.Property(static x => x.MonthLabel).HasMaxLength(12).IsRequired();
            _ = b.HasIndex(static x => x.CalendarDate).IsUnique();
        });

        _ = modelBuilder.Entity<DimBranch>(static b =>
        {
            _ = b.ToTable("dim_branch");
            _ = b.HasKey(static x => new { x.TenantId, x.BranchId });
            _ = b.Property(static x => x.BranchName).HasMaxLength(160).IsRequired();
            _ = b.Property(static x => x.ShortCode).HasMaxLength(32);
        });

        _ = modelBuilder.Entity<DimMetric>(static b =>
        {
            _ = b.ToTable("dim_metric");
            _ = b.HasKey(static x => new { x.TenantId, x.MetricId });
            _ = b.Property(static x => x.MetricKey).HasMaxLength(120).IsRequired();
            _ = b.Property(static x => x.Title).HasMaxLength(180).IsRequired();
            _ = b.Property(static x => x.UnitLabel).HasMaxLength(40);
            _ = b.Property(static x => x.SourceType).HasMaxLength(24).IsRequired();
        });

        _ = modelBuilder.Entity<FactMetricDaily>(static b =>
        {
            _ = b.ToTable("fact_metric_daily");
            _ = b.HasKey(static x => x.Id);
            _ = b.Property(static x => x.PartitionLabel).HasMaxLength(160).IsRequired();
            _ = b.Property(static x => x.MeasureSum).HasPrecision(18, 4);
            _ = b.Property(static x => x.GoalSum).HasPrecision(18, 4);
            _ = b.HasIndex(static x => new { x.DateKey, x.MetricId });
        });

        _ = modelBuilder.Entity<FactRevenueMonthly>(static b =>
        {
            _ = b.ToTable("fact_revenue_monthly");
            _ = b.HasKey(static x => x.Id);
            _ = b.Property(static x => x.MonthLabel).HasMaxLength(12).IsRequired();
            _ = b.Property(static x => x.TaxDeductibleGiving).HasPrecision(18, 2);
            _ = b.Property(static x => x.NonDeductibleDuesAndFees).HasPrecision(18, 2);
            _ = b.HasIndex(static x => new { x.MonthDateKey, x.BranchId });
        });

        _ = modelBuilder.Entity<FactAttendanceWeekly>(static b =>
        {
            _ = b.ToTable("fact_attendance_weekly");
            _ = b.HasKey(static x => x.Id);
            _ = b.HasIndex(static x => new { x.WeekEndDateKey, x.BranchId });
        });
    }
}
