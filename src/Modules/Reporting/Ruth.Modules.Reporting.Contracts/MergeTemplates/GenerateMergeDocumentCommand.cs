using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.MergeTemplates;

/// <summary>Generates a merged HTML or DOCX document from a Report, DataView, or explicit row contexts.</summary>
/// <param name="MergeTemplateId">Target MergeTemplate identifier.</param>
/// <param name="ReportId">Optional Report supplying merged rows.</param>
/// <param name="DataViewId">Optional DataView supplying merged rows when ReportId is null.</param>
/// <param name="ExplicitRows">Optional explicit row dictionaries for ad-hoc merge generation.</param>
/// <param name="GlobalParameters">Optional global merge tokens available to header, body, and footer.</param>
/// <param name="FormatOverride">Optional format override (Html or Docx).</param>
public sealed record GenerateMergeDocumentCommand(
    Guid MergeTemplateId,
    Guid? ReportId = null,
    Guid? DataViewId = null,
    IReadOnlyList<IReadOnlyDictionary<string, object?>>? ExplicitRows = null,
    IReadOnlyDictionary<string, object?>? GlobalParameters = null,
    MergeTemplateFormat? FormatOverride = null
) : ICommand<ReportingFileExportResponse>;
