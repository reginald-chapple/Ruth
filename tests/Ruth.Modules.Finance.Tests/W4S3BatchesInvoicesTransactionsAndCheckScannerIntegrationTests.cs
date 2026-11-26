using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S3BatchesInvoicesTransactionsAndCheckScannerIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task Invoices_Batches_MultiSplitTransactions_OutboxEvents_AndRefunds_WorkEndToEnd()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var (deductibleId, duesId) = await ResolveSeededAccountsAsync(queries);
        var personId = Guid.CreateVersion7();
        var (invoiceId, batchId, txId) = await CreateInvoiceBatchAndSettlementAsync(
            commands,
            personId,
            deductibleId,
            duesId
        );

        var settledInvoice = (
            await queries.QueryAsync<GetInvoiceByIdQuery, InvoiceResponse>(
                new GetInvoiceByIdQuery(invoiceId)
            )
        ).Value;
        Assert.Equal(InvoiceStatus.Paid, settledInvoice.Status);

        var closedBatch = await commands.SendAsync<
            CloseFinancialBatchCommand,
            FinancialBatchResponse
        >(new CloseFinancialBatchCommand(batchId, AuditNote: "Verified deposit"));
        Assert.True(closedBatch.IsSuccess);
        Assert.Equal(FinancialBatchStatus.Closed, closedBatch.Value.Status);

        var lockedAttempt = await commands.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                personId,
                "USD",
                FinancialTransactionSourceType.StaffEntry,
                FinancialTenderType.Cash,
                [new FinancialTransactionSplitSpec(deductibleId, 10.00m)],
                BatchId: batchId
            )
        );
        Assert.True(lockedAttempt.IsFailure);

        await VerifyReopenRefundAndOutboxAsync(
            commands,
            queries,
            dbContext,
            batchId,
            txId,
            invoiceId
        );
    }

    private static async Task<(Guid DeductibleId, Guid DuesId)> ResolveSeededAccountsAsync(
        IQueryDispatcher queries
    )
    {
        var accounts = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery())
        ).Value;
        return (
            accounts.First(static a => a.IsTaxDeductible).Id,
            accounts.First(static a => !a.IsTaxDeductible).Id
        );
    }

    private static async Task<(
        Guid InvoiceId,
        Guid BatchId,
        Guid TxId
    )> CreateInvoiceBatchAndSettlementAsync(
        ICommandDispatcher commands,
        Guid personId,
        Guid deductibleId,
        Guid duesId
    )
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var invoice = (
            await commands.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
                new CreateInvoiceCommand(
                    personId,
                    today,
                    today.AddDays(30),
                    "USD",
                    [new InvoiceLineItemSpec(duesId, "Annual Membership", 1m, 150.00m)],
                    IssueImmediately: true
                )
            )
        ).Value;

        var batch = (
            await commands.SendAsync<CreateFinancialBatchCommand, FinancialBatchResponse>(
                new CreateFinancialBatchCommand(
                    "Deposit Batch #1",
                    "USD",
                    ControlAmount: 250.00m,
                    ControlItemCount: 1,
                    OpenImmediately: true
                )
            )
        ).Value;

        var tx = (
            await commands.SendAsync<
                RecordFinancialTransactionCommand,
                FinancialTransactionResponse
            >(
                new RecordFinancialTransactionCommand(
                    personId,
                    "USD",
                    FinancialTransactionSourceType.StaffEntry,
                    FinancialTenderType.Check,
                    [
                        new FinancialTransactionSplitSpec(duesId, 150.00m, invoice.Id, "Dues"),
                        new FinancialTransactionSplitSpec(deductibleId, 100.00m, Summary: "Gift"),
                    ],
                    BatchId: batch.Id,
                    TransactionCode: "CHK-501"
                )
            )
        ).Value;

        return (invoice.Id, batch.Id, tx.Id);
    }

    private static async Task VerifyReopenRefundAndOutboxAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        FinanceDbContext dbContext,
        Guid batchId,
        Guid transactionId,
        Guid invoiceId
    )
    {
        var reopened = await commands.SendAsync<
            ReopenFinancialBatchCommand,
            FinancialBatchResponse
        >(
            new ReopenFinancialBatchCommand(
                batchId,
                ReopenedByPersonId: null,
                "Customer requested partial dues refund"
            )
        );
        Assert.True(reopened.IsSuccess);

        var refunded = await commands.SendAsync<
            RefundFinancialTransactionCommand,
            FinancialTransactionResponse
        >(new RefundFinancialTransactionCommand(transactionId, 50.00m, "Partial dues credit"));
        Assert.True(refunded.IsSuccess);
        Assert.Equal(FinancialTransactionStatus.PartiallyRefunded, refunded.Value.Status);

        var reopenedInvoice = (
            await queries.QueryAsync<GetInvoiceByIdQuery, InvoiceResponse>(
                new GetInvoiceByIdQuery(invoiceId)
            )
        ).Value;
        Assert.Equal(InvoiceStatus.PartiallyPaid, reopenedInvoice.Status);
        Assert.Equal(30.00m, reopenedInvoice.BalanceDue);

        var outboxCount = await dbContext.OutboxMessages.CountAsync();
        Assert.True(outboxCount >= 3);
    }
}
