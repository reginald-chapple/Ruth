using System.Text;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Statements;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class DownloadFinancialStatementQueryHandler(
    FinanceDbContext dbContext,
    IStorageProvider storageProvider,
    StatementLedgerQueryService ledgerQueryService,
    IFinancialStatementRenderer renderer
) : IQueryHandler<DownloadFinancialStatementQuery, FinancialStatementDownloadResponse>
{
    public async Task<Result<FinancialStatementDownloadResponse>> HandleAsync(
        DownloadFinancialStatementQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = GeneratedFinancialStatementId.From(query.StatementId);
        var statement = await dbContext
            .GeneratedStatements.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (statement is null)
        {
            return FinanceErrors.StatementNotFound;
        }

        var isPdf = query.Format == StatementDownloadFormat.Pdf;
        var cached = await TryReadArchivedAsync(statement, isPdf, cancellationToken);
        return cached is not null
            ? cached
            : await RenderOnDemandAsync(statement, isPdf, cancellationToken);
    }

    private async Task<FinancialStatementDownloadResponse?> TryReadArchivedAsync(
        GeneratedFinancialStatement statement,
        bool isPdf,
        CancellationToken cancellationToken
    )
    {
        var storageKey = isPdf ? statement.PdfStorageKey : statement.HtmlStorageKey;
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        await using var stream = await storageProvider.OpenReadAsync(storageKey, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return new FinancialStatementDownloadResponse(
            $"{statement.StatementNumber}.{(isPdf ? "pdf" : "html")}",
            isPdf ? "application/pdf" : "text/html; charset=utf-8",
            ms.ToArray()
        );
    }

    private async Task<Result<FinancialStatementDownloadResponse>> RenderOnDemandAsync(
        GeneratedFinancialStatement statement,
        bool isPdf,
        CancellationToken cancellationToken
    )
    {
        var template = await dbContext
            .StatementTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == statement.TemplateId, cancellationToken);
        if (template is null)
        {
            return FinanceErrors.StatementTemplateNotFound;
        }

        var memberIds = statement.TargetPersonId.HasValue
            ? new HashSet<Guid> { statement.TargetPersonId.Value }
            : [];
        var ledger = await ledgerQueryService.BuildStatementLedgerAsync(
            template,
            memberIds,
            statement.TargetHouseholdId,
            statement.PeriodStartDate,
            statement.PeriodEndDate,
            cancellationToken
        );
        var rendered = await renderer.RenderAsync(template, statement, ledger, cancellationToken);
        if (rendered.IsFailure)
        {
            return Result.Failure<FinancialStatementDownloadResponse>(rendered.Errors);
        }

        var payload = isPdf
            ? rendered.Value.PdfBytes
            : Encoding.UTF8.GetBytes(rendered.Value.HtmlContent);
        return new FinancialStatementDownloadResponse(
            $"{statement.StatementNumber}.{(isPdf ? "pdf" : "html")}",
            isPdf ? "application/pdf" : "text/html; charset=utf-8",
            payload
        );
    }
}
