using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S5AssistanceIntegrationTests(FinancePostgresContainerFixture postgres)
{
    [Fact]
    public async Task AssistanceProgram_CaseIntake_VerifiedDocs_InvoiceFeeWaiver_AndPersonMerge_WorkEndToEnd()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var accounts = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery(ActiveOnly: true))
        ).Value;
        var fundingAccountId = accounts.First(static a => a.IsTaxDeductible).Id;
        var duesAccountId = accounts.First(static a => !a.IsTaxDeductible).Id;
        var applicantPersonId = Guid.CreateVersion7();

        var (caseId, invoiceId) = await ExecuteFeeWaiverCaseAsync(
            commands,
            fundingAccountId,
            duesAccountId,
            applicantPersonId
        );

        var updatedInvoice = (
            await queries.QueryAsync<GetInvoiceByIdQuery, InvoiceResponse>(
                new GetInvoiceByIdQuery(invoiceId)
            )
        ).Value;
        Assert.Equal(InvoiceStatus.Paid, updatedInvoice.Status);
        Assert.Equal(0m, updatedInvoice.BalanceDue);

        await W4S5AssistanceMergeTestHelper.VerifyOutboxAndPersonMergeAsync(
            queries,
            dbContext,
            scope.ServiceProvider,
            caseId,
            applicantPersonId
        );
    }

    private static async Task<(Guid CaseId, Guid InvoiceId)> ExecuteFeeWaiverCaseAsync(
        ICommandDispatcher commands,
        Guid fundingAccountId,
        Guid duesAccountId,
        Guid applicantPersonId
    )
    {
        var invoice = (
            await commands.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
                new CreateInvoiceCommand(
                    applicantPersonId,
                    new DateOnly(2026, 6, 1),
                    new DateOnly(2026, 7, 1),
                    "USD",
                    [new InvoiceLineItemSpec(duesAccountId, "Program Tuition", 1m, 400.00m)],
                    IssueImmediately: true
                )
            )
        ).Value;

        var program = (
            await commands.SendAsync<CreateAssistanceProgramCommand, AssistanceProgramResponse>(
                new CreateAssistanceProgramCommand(
                    new AssistanceProgramSpec(
                        "Tuition Fee Waiver & Scholarship",
                        "TUITION-WAIVER-26",
                        AssistanceProgramCategory.FeeWaiver,
                        fundingAccountId,
                        TotalBudgetAmount: 5000.00m,
                        MaxAwardPerCase: 1000.00m,
                        RequiresDocumentation: true
                    )
                )
            )
        ).Value;

        var submitted = (
            await commands.SendAsync<SubmitAssistanceCaseCommand, AssistanceCaseResponse>(
                new SubmitAssistanceCaseCommand(
                    program.Id,
                    applicantPersonId,
                    400.00m,
                    "Requesting full tuition waiver"
                )
            )
        ).Value;

        _ = await commands.SendAsync<AttachAssistanceDocumentCommand, AssistanceCaseResponse>(
            new AttachAssistanceDocumentCommand(
                submitted.Id,
                "Scholarship Eligibility Form",
                "Application",
                IsVerified: true
            )
        );

        var approved = (
            await commands.SendAsync<ReviewAssistanceCaseCommand, AssistanceCaseResponse>(
                new ReviewAssistanceCaseCommand(
                    submitted.Id,
                    AssistanceCaseStatus.Approved,
                    ApprovedAmount: 400.00m,
                    DecisionNotes: "Approved for full tuition waiver"
                )
            )
        ).Value;
        Assert.Equal(AssistanceCaseStatus.Approved, approved.Status);

        var disbursed = (
            await commands.SendAsync<RecordAssistanceDisbursementCommand, AssistanceCaseResponse>(
                new RecordAssistanceDisbursementCommand(
                    submitted.Id,
                    AssistanceDisbursementMethod.InternalFeeWaiverCredit,
                    400.00m,
                    "Internal Tuition Ledger",
                    TargetInvoiceId: invoice.Id
                )
            )
        ).Value;
        Assert.Equal(AssistanceCaseStatus.Disbursed, disbursed.Status);
        return (submitted.Id, invoice.Id);
    }
}
