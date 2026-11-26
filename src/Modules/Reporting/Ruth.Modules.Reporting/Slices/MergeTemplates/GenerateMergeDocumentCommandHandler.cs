using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.MergeTemplates;
using Ruth.Modules.Reporting.Domain.Reports;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.MergeTemplates;

internal sealed class GenerateMergeDocumentCommandHandler(
    ReportingDbContext db,
    IMergeTemplateEngine mergeEngine,
    IQueryHandler<ExecuteReportQuery, ReportExecutionResponse> executeReport,
    IQueryHandler<ExecuteDataViewQuery, DataViewExecutionResponse> executeDataView
) : ICommandHandler<GenerateMergeDocumentCommand, ReportingFileExportResponse>
{
    public async Task<Result<ReportingFileExportResponse>> HandleAsync(
        GenerateMergeDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var templateId = MergeTemplateId.From(command.MergeTemplateId);
        var template = await db
            .MergeTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (template is null)
        {
            return Result.Failure<ReportingFileExportResponse>(
                ReportingErrors.NotFound("MergeTemplate.NotFound", "MergeTemplate was not found.")
            );
        }

        var rowsRes = await ResolveRowsAsync(command, cancellationToken);
        if (rowsRes.IsFailure)
        {
            return Result.Failure<ReportingFileExportResponse>(rowsRes.PrimaryError);
        }

        var format = command.FormatOverride ?? template.Format;
        var renderedRes = await RenderSectionsAsync(
            template,
            rowsRes.Value,
            command.GlobalParameters,
            format,
            cancellationToken
        );
        return renderedRes.IsFailure
            ? Result.Failure<ReportingFileExportResponse>(renderedRes.PrimaryError)
            : Result.Success(
                PackageOutput(template.Name, format, renderedRes.Value, rowsRes.Value.Count)
            );
    }

    private async Task<Result<List<string>>> RenderSectionsAsync(
        MergeTemplate template,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyDictionary<string, object?>? globalParameters,
        MergeTemplateFormat format,
        CancellationToken ct
    )
    {
        var mode =
            format == MergeTemplateFormat.Html
                ? TemplateOutputMode.Html
                : TemplateOutputMode.PlainText;
        var options = new MergeRenderOptions(OutputMode: mode);
        var globals = globalParameters
            ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        List<string> sections = [];
        if (!string.IsNullOrWhiteSpace(template.HeaderMarkup))
        {
            var hdr = await mergeEngine.RenderAsync(template.HeaderMarkup, globals, options, ct);
            sections.Add(hdr.IsSuccess ? hdr.Value.RenderedContent : template.HeaderMarkup);
        }

        foreach (var row in rows)
        {
            var body = await mergeEngine.RenderAsync(
                template.TemplateBody,
                MergeContexts(globals, row),
                options,
                ct
            );
            if (body.IsFailure)
            {
                return Result.Failure<List<string>>(body.PrimaryError);
            }

            sections.Add(body.Value.RenderedContent);
        }

        if (!string.IsNullOrWhiteSpace(template.FooterMarkup))
        {
            var ftr = await mergeEngine.RenderAsync(template.FooterMarkup, globals, options, ct);
            sections.Add(ftr.IsSuccess ? ftr.Value.RenderedContent : template.FooterMarkup);
        }

        return Result.Success(sections);
    }

    private static ReportingFileExportResponse PackageOutput(
        string templateName,
        MergeTemplateFormat format,
        IReadOnlyList<string> sections,
        int rowCount
    )
    {
        var fileNameBase = ReportingTextNormalizer.SanitizeFileName(templateName, "merge-doc");
        if (format == MergeTemplateFormat.Docx)
        {
            var docxBytes = ReportingDocumentPackager.BuildWordDocx(templateName, sections);
            return new ReportingFileExportResponse(
                $"{fileNameBase}.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                docxBytes,
                rowCount
            );
        }

        var html = BuildHtmlDocument(templateName, sections);
        return new ReportingFileExportResponse(
            $"{fileNameBase}.html",
            "text/html; charset=utf-8",
            Encoding.UTF8.GetBytes(html),
            rowCount
        );
    }

    private async Task<
        Result<IReadOnlyList<IReadOnlyDictionary<string, object?>>>
    > ResolveRowsAsync(GenerateMergeDocumentCommand command, CancellationToken ct)
    {
        if (command.ExplicitRows is { Count: > 0 })
        {
            return Result.Success(command.ExplicitRows);
        }

        if (command.ReportId is { } reportId && reportId != Guid.Empty)
        {
            var exec = await executeReport.HandleAsync(new ExecuteReportQuery(reportId), ct);
            if (exec.IsFailure)
            {
                return Result.Failure<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(
                    exec.PrimaryError
                );
            }

            List<IReadOnlyDictionary<string, object?>> mapped =
            [
                .. exec.Value.Rows.Select(static r =>
                    (IReadOnlyDictionary<string, object?>)
                        r.ToDictionary(
                            static kv => kv.Key,
                            static kv => (object?)kv.Value,
                            StringComparer.OrdinalIgnoreCase
                        )
                ),
            ];
            return Result.Success<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(mapped);
        }

        if (command.DataViewId is { } dvId && dvId != Guid.Empty)
        {
            var exec = await executeDataView.HandleAsync(new ExecuteDataViewQuery(dvId), ct);
            if (exec.IsFailure)
            {
                return Result.Failure<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(
                    exec.PrimaryError
                );
            }

            List<IReadOnlyDictionary<string, object?>> mapped =
            [
                .. exec.Value.Records.Select(static r => r.Values),
            ];
            return Result.Success<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(mapped);
        }

        return Result.Success<IReadOnlyList<IReadOnlyDictionary<string, object?>>>([]);
    }

    private static Dictionary<string, object?> MergeContexts(
        IReadOnlyDictionary<string, object?> globals,
        IReadOnlyDictionary<string, object?> row
    )
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in globals)
        {
            merged[k] = v;
        }

        var rowCopy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in row)
        {
            merged[k] = v;
            rowCopy[k] = v;
            var compact = k.Replace(" ", string.Empty, StringComparison.Ordinal);
            if (!string.Equals(compact, k, StringComparison.Ordinal))
            {
                merged[compact] = v;
                rowCopy[compact] = v;
            }
        }

        merged["Row"] = rowCopy;
        return merged;
    }

    private static string BuildHtmlDocument(string title, IReadOnlyList<string> sections)
    {
        var encodedTitle = WebUtility.HtmlEncode(title);
        var body = string.Join("\n<hr class=\"ruth-merge-divider\" />\n", sections);
        return "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\" />\n<title>"
            + encodedTitle
            + "</title>\n</head>\n<body>\n"
            + body
            + "\n</body>\n</html>";
    }
}
