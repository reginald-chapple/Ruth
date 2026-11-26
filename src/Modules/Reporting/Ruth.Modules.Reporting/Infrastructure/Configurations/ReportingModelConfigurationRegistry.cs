using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal static class ReportingModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new DataViewConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DataViewFilterConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DataViewPersistedValueConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ReportConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ReportFieldConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MergeTemplateConfiguration());

        _ = modelBuilder.ApplyConfiguration(new MetricCategoryConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MetricConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MetricPartitionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MetricValueConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MetricValuePartitionConfiguration());

        ReportingStarSchemaConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
