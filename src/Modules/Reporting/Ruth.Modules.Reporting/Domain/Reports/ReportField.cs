using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Domain.Reports;

/// <summary>Ordered column projection definition belonging to a Report.</summary>
public sealed class ReportField : Entity<ReportFieldId>
{
    private ReportField() { }

    private ReportField(ReportFieldId id)
        : base(id) { }

    public ReportId ReportId { get; private set; }
    public string FieldSelectKey { get; private set; } = string.Empty;
    public string ColumnHeaderText { get; private set; } = string.Empty;
    public int ColumnOrder { get; private set; }
    public string? SelectionConfigJson { get; private set; }
    public int? SortOrder { get; private set; }
    public ReportSortDirection SortDirection { get; private set; }
    public bool ShowInGrid { get; private set; } = true;

    public static Result<ReportField> Create(
        ReportId reportId,
        ReportFieldSpec spec,
        int columnOrder
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        return string.IsNullOrWhiteSpace(spec.FieldSelectKey)
                ? Result.Failure<ReportField>(
                    ReportingErrors.Validation(
                        "ReportField.PluginKeyRequired",
                        "ReportField FieldSelectKey is required."
                    )
                )
            : string.IsNullOrWhiteSpace(spec.ColumnHeaderText)
                ? Result.Failure<ReportField>(
                    ReportingErrors.Validation(
                        "ReportField.HeaderRequired",
                        "ReportField ColumnHeaderText is required."
                    )
                )
            : Result.Success(
                new ReportField(ReportFieldId.Create())
                {
                    ReportId = reportId,
                    FieldSelectKey = spec.FieldSelectKey.Trim(),
                    ColumnHeaderText = spec.ColumnHeaderText.Trim(),
                    ColumnOrder = columnOrder,
                    SelectionConfigJson = string.IsNullOrWhiteSpace(spec.SelectionConfigJson)
                        ? null
                        : spec.SelectionConfigJson.Trim(),
                    SortOrder = spec.SortOrder,
                    SortDirection = spec.SortDirection,
                    ShowInGrid = spec.ShowInGrid,
                }
            );
    }
}
