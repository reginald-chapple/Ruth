using System.Globalization;
using System.Text;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Statements;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class StatementGenerationCollaborator(
    FinanceDbContext dbContext,
    StatementRecipientResolver recipientResolver,
    StatementLedgerQueryService ledgerQueryService,
    IFinancialStatementRenderer renderer,
    IStorageProvider storageProvider,
    IClock clock
)
{
    public async Task<Result<RenderedFinancialStatementDocumentResponse>> GenerateAndPersistAsync(
        FinancialStatementTemplate template,
        FinancialStatementRecipientScope scope,
        Guid? targetPersonId,
        Guid? targetHouseholdId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        bool persistArchiveToStorage,
        Guid? batchRunId,
        CancellationToken cancellationToken
    )
    {
        if (periodEndDate < periodStartDate)
        {
            return FinanceErrors.StatementPeriodInvalid;
        }

        var recipientResult = await recipientResolver.ResolveAsync(
            scope,
            targetPersonId,
            targetHouseholdId,
            cancellationToken
        );
        if (recipientResult.IsFailure)
        {
            return Result.Failure<RenderedFinancialStatementDocumentResponse>(
                recipientResult.Errors
            );
        }

        var (displayName, memberPersonIds, resolvedHouseholdId, primaryPersonId) =
            recipientResult.Value;

        var ledger = await ledgerQueryService.BuildStatementLedgerAsync(
            template,
            memberPersonIds,
            resolvedHouseholdId,
            periodStartDate,
            periodEndDate,
            cancellationToken
        );

        return await RenderAndSaveStatementAsync(
            template,
            scope,
            primaryPersonId,
            resolvedHouseholdId,
            displayName,
            periodStartDate,
            periodEndDate,
            persistArchiveToStorage,
            batchRunId,
            ledger,
            cancellationToken
        );
    }

    private async Task<
        Result<RenderedFinancialStatementDocumentResponse>
    > RenderAndSaveStatementAsync(
        FinancialStatementTemplate template,
        FinancialStatementRecipientScope scope,
        Guid? primaryPersonId,
        Guid? resolvedHouseholdId,
        string displayName,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        bool persistArchiveToStorage,
        Guid? batchRunId,
        AggregatedStatementLedger ledger,
        CancellationToken cancellationToken
    )
    {
        var createdStmt = GeneratedFinancialStatement.Create(
            template.Id,
            template.StatementKind,
            scope,
            primaryPersonId,
            resolvedHouseholdId,
            displayName,
            periodStartDate,
            periodEndDate,
            template.CurrencyCode,
            ledger.TaxDeductibleTotal,
            ledger.NonDeductiblePaidTotal,
            ledger.OpenInvoiceBalanceTotal,
            ledger.QualifyingTransactionCount,
            clock.UtcNow,
            batchRunId
        );
        if (createdStmt.IsFailure)
        {
            return Result.Failure<RenderedFinancialStatementDocumentResponse>(createdStmt.Errors);
        }

        var statement = createdStmt.Value;
        var rendered = await renderer.RenderAsync(template, statement, ledger, cancellationToken);
        if (rendered.IsFailure)
        {
            return Result.Failure<RenderedFinancialStatementDocumentResponse>(rendered.Errors);
        }

        if (persistArchiveToStorage)
        {
            await PersistArchiveObjectsAsync(
                statement,
                rendered.Value.HtmlContent,
                rendered.Value.PdfBytes,
                cancellationToken
            );
        }

        _ = dbContext.GeneratedStatements.Add(statement);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new RenderedFinancialStatementDocumentResponse(
            StatementMapper.ToResponse(statement, template.Name),
            rendered.Value.HtmlContent,
            rendered.Value.PdfBytes,
            ledger.TaxDeductibleLines,
            ledger.NonDeductibleLines,
            ledger.InvoiceSummaries,
            ledger.PledgeSummaries
        );
    }

    private async Task PersistArchiveObjectsAsync(
        GeneratedFinancialStatement statement,
        string htmlContent,
        byte[] pdfBytes,
        CancellationToken cancellationToken
    )
    {
        var yearText = statement.PeriodEndDate.Year.ToString(CultureInfo.InvariantCulture);
        var htmlKey = TenantStorageKeyScope.ResolveKey(
            AmbientTenantContext.Instance,
            $"finance/statements/{yearText}/{statement.StatementNumber}.html"
        );
        var pdfKey = TenantStorageKeyScope.ResolveKey(
            AmbientTenantContext.Instance,
            $"finance/statements/{yearText}/{statement.StatementNumber}.pdf"
        );

        await using (var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlContent)))
        {
            _ = await storageProvider.UploadAsync(
                htmlKey,
                htmlStream,
                "text/html; charset=utf-8",
                cancellationToken
            );
        }

        await using (var pdfStream = new MemoryStream(pdfBytes))
        {
            _ = await storageProvider.UploadAsync(
                pdfKey,
                pdfStream,
                "application/pdf",
                cancellationToken
            );
        }

        statement.AttachStorageKeys(htmlKey, pdfKey);
    }
}
