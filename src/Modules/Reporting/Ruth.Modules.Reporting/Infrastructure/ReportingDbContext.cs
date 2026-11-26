using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Domain.Analytics;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Domain.MergeTemplates;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Domain.Reports;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Reporting.Infrastructure;

/// <summary>EF Core database context exclusively owning the reporting PostgreSQL schema.</summary>
public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>PostgreSQL schema name owned by the Reporting module.</summary>
    public const string SchemaName = "reporting";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    public DbSet<DataView> DataViews => Set<DataView>();
    public DbSet<DataViewFilter> DataViewFilters => Set<DataViewFilter>();
    public DbSet<DataViewPersistedValue> DataViewPersistedValues => Set<DataViewPersistedValue>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportField> ReportFields => Set<ReportField>();
    public DbSet<MergeTemplate> MergeTemplates => Set<MergeTemplate>();

    public DbSet<MetricCategory> MetricCategories => Set<MetricCategory>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<MetricPartition> MetricPartitions => Set<MetricPartition>();
    public DbSet<MetricValue> MetricValues => Set<MetricValue>();
    public DbSet<MetricValuePartition> MetricValuePartitions => Set<MetricValuePartition>();

    public DbSet<DimDate> DimDates => Set<DimDate>();
    public DbSet<DimBranch> DimBranches => Set<DimBranch>();
    public DbSet<DimMetric> DimMetrics => Set<DimMetric>();
    public DbSet<FactMetricDaily> FactMetricsDaily => Set<FactMetricDaily>();
    public DbSet<FactRevenueMonthly> FactRevenuesMonthly => Set<FactRevenueMonthly>();
    public DbSet<FactAttendanceWeekly> FactAttendancesWeekly => Set<FactAttendanceWeekly>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.ReportingModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
