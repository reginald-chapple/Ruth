using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S5StatementsIntegrationTests(FinancePostgresContainerFixture postgres)
{
    [Fact]
    public async Task Statements_BatchRuns_Downloads_AndHtmlHintExports_SeparateDeductibleAndNonDeductible()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var accounts = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery(ActiveOnly: true))
        ).Value;
        var deductibleAccount = accounts.First(static a => a.IsTaxDeductible);
        var duesAccount = accounts.First(static a => !a.IsTaxDeductible);

        var (adultA, householdId, sponsorOrgId) =
            await W4S5StatementTestSeedHelper.SeedHouseholdAndSponsorLedgerActivityAsync(
                commands,
                deductibleAccount.Id,
                duesAccount.Id
            );

        await VerifyGenerationBatchAndExportsAsync(
            commands,
            queries,
            adultA,
            householdId,
            sponsorOrgId
        );
    }

    private static async Task VerifyGenerationBatchAndExportsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid individualPersonId,
        Guid householdId,
        Guid sponsorOrgId
    )
    {
        var combinedTemplate = (
            await commands.SendAsync<
                CreateFinancialStatementTemplateCommand,
                FinancialStatementTemplateResponse
            >(
                new CreateFinancialStatementTemplateCommand(
                    new FinancialStatementTemplateSpec(
                        "2026 Combined Separated Statement & Tax Receipt",
                        FinancialStatementKind.CombinedSeparatedStatement,
                        "Ruth Community Foundation",
                        OrganizationTaxId: "84-7654321",
                        HeaderMergeTemplate: "<p>Statement for {{ RecipientName }} ({{ RecipientScope }})</p>",
                        FooterMergeTemplate: "<p>Deductible: {{ TaxDeductibleTotal }} {{ CurrencyCode }} | Non-Deductible: {{ NonDeductiblePaidTotal }} {{ CurrencyCode }}</p>",
                        IsDefault: true
                    )
                )
            )
        ).Value;

        var hhRendered = (
            await commands.SendAsync<
                GenerateFinancialStatementCommand,
                RenderedFinancialStatementDocumentResponse
            >(
                new GenerateFinancialStatementCommand(
                    combinedTemplate.Id,
                    FinancialStatementRecipientScope.Household,
                    TargetPersonId: null,
                    TargetHouseholdId: householdId,
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 12, 31)
                )
            )
        ).Value;

        Assert.Equal(500.00m, hhRendered.Statement.TaxDeductibleTotal);
        Assert.Equal(75.00m, hhRendered.Statement.NonDeductiblePaidTotal);
        Assert.Contains(
            "id=\"tax-deductible-receipt-section\"",
            hhRendered.HtmlContent,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "id=\"non-deductible-statement-section\"",
            hhRendered.HtmlContent,
            StringComparison.Ordinal
        );

        await VerifyPdfAndBatchAsync(
            commands,
            queries,
            combinedTemplate.Id,
            hhRendered.Statement.Id,
            sponsorOrgId
        );
        await ExportHtmlForLintAsync(commands, queries, individualPersonId, hhRendered.HtmlContent);
    }

    private static async Task VerifyPdfAndBatchAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid combinedTemplateId,
        Guid hhStatementId,
        Guid sponsorOrgId
    )
    {
        var pdfDownload = (
            await queries.QueryAsync<
                DownloadFinancialStatementQuery,
                FinancialStatementDownloadResponse
            >(new DownloadFinancialStatementQuery(hhStatementId, StatementDownloadFormat.Pdf))
        ).Value;
        Assert.Equal("application/pdf", pdfDownload.ContentType);
        Assert.StartsWith(
            "%PDF-1.4",
            Encoding.ASCII.GetString(pdfDownload.ContentBytes),
            StringComparison.Ordinal
        );

        var batchRun = (
            await commands.SendAsync<
                RunFinancialStatementBatchCommand,
                FinancialStatementBatchRunResponse
            >(
                new RunFinancialStatementBatchCommand(
                    combinedTemplateId,
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 12, 31),
                    ConsolidateHouseholds: true,
                    IncludeOrganizations: true
                )
            )
        ).Value;
        Assert.Equal(2, batchRun.GeneratedCount);
        Assert.Equal(3000.00m, batchRun.TaxDeductibleAggregateTotal);
        Assert.Contains(batchRun.Statements, s =>
            s.RecipientScope == FinancialStatementRecipientScope.OrganizationSponsor
            && s.TargetPersonId == sponsorOrgId
        );
    }

    private static async Task ExportHtmlForLintAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid individualPersonId,
        string combinedHtml
    )
    {
        var templates = (
            await queries.QueryAsync<
                GetFinancialStatementTemplatesQuery,
                IReadOnlyList<FinancialStatementTemplateResponse>
            >(new GetFinancialStatementTemplatesQuery(ActiveOnly: true))
        ).Value;
        var deductibleTemplate = templates.First(static t =>
            t.StatementKind == FinancialStatementKind.TaxDeductibleReceipt
        );
        var indivReceipt = (
            await commands.SendAsync<
                GenerateFinancialStatementCommand,
                RenderedFinancialStatementDocumentResponse
            >(
                new GenerateFinancialStatementCommand(
                    deductibleTemplate.Id,
                    FinancialStatementRecipientScope.Individual,
                    individualPersonId,
                    TargetHouseholdId: null,
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 12, 31)
                )
            )
        ).Value;

        var exportDir = Path.Combine(FindRepoRoot(), "artifacts", "htmlhint");
        _ = Directory.CreateDirectory(exportDir);
        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "w4s5-combined-household-statement.html"),
            combinedHtml
        );
        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "w4s5-individual-tax-receipt.html"),
            indivReceipt.HtmlContent
        );
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ruth.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
