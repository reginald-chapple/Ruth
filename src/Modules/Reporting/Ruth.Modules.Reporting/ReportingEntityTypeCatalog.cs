using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Domain.MergeTemplates;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Domain.Reports;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Reporting;

internal static class ReportingEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        new(
            ReportingSystemGuids.DataView,
            DataView.EntityTypeKeyName,
            nameof(DataView),
            "DataView",
            "Reporting",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            ReportingSystemGuids.Report,
            Report.EntityTypeKeyName,
            nameof(Report),
            "Report",
            "Reporting",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            ReportingSystemGuids.MergeTemplate,
            MergeTemplate.EntityTypeKeyName,
            nameof(MergeTemplate),
            "Merge Template",
            "Reporting",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            ReportingSystemGuids.MetricCategory,
            MetricCategory.EntityTypeKeyName,
            nameof(MetricCategory),
            "Metric Category",
            "Reporting",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
        new(
            ReportingSystemGuids.Metric,
            Metric.EntityTypeKeyName,
            nameof(Metric),
            "Metric",
            "Reporting",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
    ];

    public static IReadOnlyList<EntityTypeRegistration> GetAll()
    {
        return _all;
    }
}
