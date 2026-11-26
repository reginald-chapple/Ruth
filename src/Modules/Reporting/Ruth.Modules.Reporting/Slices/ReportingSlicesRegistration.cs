using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.Modules.Reporting.Infrastructure.DataViews;
using Ruth.Modules.Reporting.Infrastructure.Jobs;
using Ruth.Modules.Reporting.Infrastructure.Reports;
using Ruth.Modules.Reporting.Slices.Analytics;
using Ruth.Modules.Reporting.Slices.DataViews;
using Ruth.Modules.Reporting.Slices.Events;
using Ruth.Modules.Reporting.Slices.MergeTemplates;
using Ruth.Modules.Reporting.Slices.Metrics;
using Ruth.Modules.Reporting.Slices.Reports;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Slices;

internal static class ReportingSlicesRegistration
{
    public static void Register(IServiceCollection services)
    {
        RegisterW6S5(services);
        RegisterW6S6(services);
    }

    private static void RegisterW6S5(IServiceCollection services)
    {
        BuiltInFieldSelectPlugins.Register(services);
        _ = services.AddScoped<ReportingDataViewExecutionEngine>();
        _ = services.AddScoped<
            ICommandHandler<CreateDataViewCommand, DataViewResponse>,
            CreateDataViewCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<UpdateDataViewCommand, DataViewResponse>,
            UpdateDataViewCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<MaterializeDataViewCommand, DataViewExecutionResponse>,
            MaterializeDataViewCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetDataViewsQuery, IReadOnlyList<DataViewResponse>>,
            GetDataViewsQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<ExecuteDataViewQuery, DataViewExecutionResponse>,
            ExecuteDataViewQueryHandler
        >();

        _ = services.AddScoped<
            ICommandHandler<CreateReportCommand, ReportResponse>,
            CreateReportCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetReportsQuery, IReadOnlyList<ReportResponse>>,
            GetReportsQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<ExecuteReportQuery, ReportExecutionResponse>,
            ExecuteReportQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<ExportReportQuery, ReportingFileExportResponse>,
            ExportReportQueryHandler
        >();

        _ = services.AddScoped<
            ICommandHandler<CreateMergeTemplateCommand, MergeTemplateResponse>,
            CreateMergeTemplateCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetMergeTemplatesQuery, IReadOnlyList<MergeTemplateResponse>>,
            GetMergeTemplatesQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<GenerateMergeDocumentCommand, ReportingFileExportResponse>,
            GenerateMergeDocumentCommandHandler
        >();

        _ = services.AddScoped<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>,
            ReportingPersonMergedConsumer
        >();
        _ = services.AddSingleton<IScheduledJob, DataViewMaterializerJob>();
    }

    private static void RegisterW6S6(IServiceCollection services)
    {
        _ = services.AddScoped<ReportingCrossSchemaAnalyticsReader>();
        _ = services.AddScoped<StarSchemaProjectionRefresher>();

        _ = services.AddScoped<
            ICommandHandler<CreateMetricCategoryCommand, MetricCategoryResponse>,
            CreateMetricCategoryCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetMetricCategoriesQuery, IReadOnlyList<MetricCategoryResponse>>,
            GetMetricCategoriesQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<CreateMetricCommand, MetricResponse>,
            CreateMetricCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetMetricsQuery, IReadOnlyList<MetricResponse>>,
            GetMetricsQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<RecordMetricValuesBatchCommand, IReadOnlyList<MetricValueResponse>>,
            RecordMetricValuesBatchCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<CalculateMetricCommand, MetricValueResponse>,
            CalculateMetricCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetMetricValuesQuery, IReadOnlyList<MetricValueResponse>>,
            GetMetricValuesQueryHandler
        >();

        _ = services.AddScoped<
            IQueryHandler<GetDynamicChartQuery, DynamicChartResponse>,
            GetDynamicChartQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetRevenueAndImpactOverviewQuery, RevenueAndImpactOverviewResponse>,
            GetRevenueAndImpactOverviewQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetExecutiveInsightsQuery, ExecutiveInsightsResponse>,
            GetExecutiveInsightsQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<RefreshStarSchemaProjectionsCommand, StarSchemaRefreshResponse>,
            RefreshStarSchemaProjectionsCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetStarSchemaStatusQuery, StarSchemaRefreshResponse>,
            GetStarSchemaStatusQueryHandler
        >();

        _ = services.AddSingleton<IScheduledJob, MetricCalculationJob>();
        _ = services.AddSingleton<IScheduledJob, StarSchemaRefreshJob>();
    }
}
