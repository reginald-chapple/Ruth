using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Domain.MergeTemplates;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Domain.Reports;

namespace Ruth.Modules.Reporting.Slices;

internal static class ReportingDtoMapper
{
    public static DataViewResponse ToResponse(DataView dv)
    {
        return new DataViewResponse(
            dv.Id.Value,
            dv.Name,
            dv.Description,
            dv.CategoryId,
            dv.EntityTypeKey,
            dv.TransformKey,
            dv.IsActive,
            dv.IsSystem,
            dv.PersistedScheduleIntervalMinutes,
            dv.PersistedLastRefreshDateTimeUtc,
            dv.PersistedLastRunDurationMilliseconds,
            dv.PersistedResultCount,
            dv.BuildFilterTree()
        );
    }

    public static ReportResponse ToResponse(Report report)
    {
        List<ReportFieldResponse> fields =
        [
            .. report
                .Fields.OrderBy(static f => f.ColumnOrder)
                .Select(static f => new ReportFieldResponse(
                    f.Id.Value,
                    f.FieldSelectKey,
                    f.ColumnHeaderText,
                    f.ColumnOrder,
                    f.SelectionConfigJson,
                    f.SortOrder,
                    f.SortDirection,
                    f.ShowInGrid
                )),
        ];

        return new ReportResponse(
            report.Id.Value,
            report.Name,
            report.Description,
            report.EntityTypeKey,
            report.DataViewId?.Value,
            report.CategoryId,
            report.FetchTop,
            report.IsActive,
            fields
        );
    }

    public static MergeTemplateResponse ToResponse(MergeTemplate tpl)
    {
        return new MergeTemplateResponse(
            tpl.Id.Value,
            tpl.Name,
            tpl.Description,
            tpl.Format,
            tpl.TemplateBody,
            tpl.HeaderMarkup,
            tpl.FooterMarkup,
            tpl.CategoryId,
            tpl.IsGlobal,
            tpl.OwnerPersonId,
            tpl.IsActive
        );
    }

    public static MetricCategoryResponse ToResponse(MetricCategory category)
    {
        return new MetricCategoryResponse(
            category.Id.Value,
            category.Name,
            category.Description,
            category.ParentCategoryId?.Value,
            category.Order
        );
    }

    public static MetricResponse ToResponse(Metric metric)
    {
        List<MetricPartitionResponse> parts =
        [
            .. metric
                .Partitions.OrderBy(static p => p.Order)
                .Select(static p => new MetricPartitionResponse(
                    p.Id.Value,
                    p.Label,
                    p.EntityTypeKey,
                    p.IsRequired,
                    p.Order
                )),
        ];

        return new MetricResponse(
            metric.Id.Value,
            metric.Title,
            metric.Subtitle,
            metric.MetricKey,
            metric.Description,
            metric.CategoryId?.Value,
            metric.IsCumulative,
            metric.SourceType,
            metric.DataViewId?.Value,
            metric.SourceQueryKey,
            metric.ScheduleCronExpression,
            metric.YAxisLabel,
            metric.UnitLabel,
            metric.IsActive,
            metric.LastRunDateTimeUtc,
            parts
        );
    }

    public static MetricValueResponse ToResponse(MetricValue value)
    {
        List<MetricPartitionValueInput> parts =
        [
            .. value.Partitions.Select(static p => new MetricPartitionValueInput(
                p.MetricPartitionId.Value,
                p.EntityId,
                p.EntityLabel
            )),
        ];

        return new MetricValueResponse(
            value.Id.Value,
            value.MetricId.Value,
            value.MetricValueType,
            value.YValue,
            value.MetricValueDateTimeUtc,
            value.XValue,
            value.Note,
            parts
        );
    }
}
