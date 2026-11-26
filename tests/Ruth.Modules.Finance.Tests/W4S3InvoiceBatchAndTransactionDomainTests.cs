using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.Transactions;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

public sealed class W4S3InvoiceBatchAndTransactionDomainTests
{
    [Fact]
    public void Invoice_EnforcesDraftOpenPartialPaidVoidAndReverseLifecycle()
    {
        var personId = Guid.CreateVersion7();
        var accountId = FinancialAccountId.Create();
        var issue = new DateOnly(2026, 3, 1);
        var due = new DateOnly(2026, 3, 31);

        Assert.True(Invoice.Create(Guid.Empty, issue, due, "USD", []).IsFailure);
        Assert.True(
            Invoice
                .Create(personId, due, issue, "USD", [(accountId, "Dues", 1m, 100m, false)])
                .IsFailure
        );

        var draft = Invoice
            .Create(
                personId,
                issue,
                due,
                "USD",
                [(accountId, "Annual Membership Dues", 2m, 75.00m, false)],
                issueImmediately: false,
                memo: "Spring renewal"
            )
            .Value;

        Assert.Equal(InvoiceStatus.Draft, draft.Status);
        Assert.Equal(150.00m, draft.SubtotalAmount);
        Assert.True(draft.ApplyPayment(50.00m).IsFailure);

        Assert.True(draft.Issue().IsSuccess);
        Assert.Equal(InvoiceStatus.Open, draft.Status);
        Assert.True(draft.IsOverdue(new DateOnly(2026, 4, 1)));
        Assert.False(draft.IsOverdue(new DateOnly(2026, 3, 15)));

        var partial = draft.ApplyPayment(50.00m);
        Assert.True(partial.IsSuccess);
        Assert.False(partial.Value);
        Assert.Equal(InvoiceStatus.PartiallyPaid, draft.Status);
        Assert.Equal(100.00m, draft.BalanceDue);
        Assert.True(draft.Void("Cannot void after payment").IsFailure);
        Assert.True(draft.ApplyPayment(150.00m).IsFailure);

        var full = draft.ApplyPayment(100.00m);
        Assert.True(full.IsSuccess);
        Assert.True(full.Value);
        Assert.Equal(InvoiceStatus.Paid, draft.Status);

        Assert.True(draft.ReversePayment(40.00m).IsSuccess);
        Assert.Equal(InvoiceStatus.PartiallyPaid, draft.Status);
        Assert.True(draft.ReversePayment(110.00m).IsSuccess);
        Assert.Equal(InvoiceStatus.Open, draft.Status);
        Assert.True(draft.Void("Member cancelled prior to term").IsSuccess);
        Assert.Equal(InvoiceStatus.Voided, draft.Status);
    }

    [Fact]
    public void FinancialBatch_EnforcesControlVarianceZero_AuditLocks_AndExplicitReopen()
    {
        var now = new DateTimeOffset(2026, 4, 10, 14, 0, 0, TimeSpan.Zero);
        Assert.True(FinancialBatch.Create("", "USD", 100m, 1, now).IsFailure);
        Assert.True(FinancialBatch.Create("Negative", "USD", -1m, 1, now).IsFailure);
        Assert.True(FinancialBatch.Create("Negative Count", "USD", 10m, -1, now).IsFailure);

        var batch = FinancialBatch.Create("Weekend Check Deposit", "USD", 250.00m, 2, now).Value;
        Assert.Equal(FinancialBatchStatus.Pending, batch.Status);
        Assert.True(batch.EnsureCanPostTransactions().IsFailure);
        Assert.True(batch.Close(250.00m, 2, Guid.CreateVersion7(), now).IsFailure);

        Assert.True(batch.Open(300.00m, 2).IsSuccess);
        Assert.Equal(FinancialBatchStatus.Open, batch.Status);
        Assert.True(batch.EnsureCanPostTransactions().IsSuccess);

        Assert.True(batch.Close(299.99m, 2, Guid.CreateVersion7(), now).IsFailure);
        Assert.True(batch.Close(300.00m, 1, Guid.CreateVersion7(), now).IsFailure);
        Assert.True(
            batch
                .Close(300.00m, 2, Guid.CreateVersion7(), now, "Balanced to deposit slip")
                .IsSuccess
        );
        Assert.Equal(FinancialBatchStatus.Closed, batch.Status);

        Assert.True(batch.EnsureCanPostTransactions().IsFailure);
        Assert.True(batch.Open(300.00m, 2).IsFailure);
        Assert.True(batch.IncrementAutomatedControl(10m).IsFailure);
        Assert.True(batch.Reopen(Guid.CreateVersion7(), auditReason: " ").IsFailure);
        Assert.True(
            batch.Reopen(Guid.CreateVersion7(), "Correct check #1042 split account").IsSuccess
        );
        Assert.Equal(FinancialBatchStatus.Open, batch.Status);
    }

    [Fact]
    public void FinancialTransaction_EnforcesSplitScale_DeductibleSnapshots_AndRefundBounds()
    {
        var now = new DateTimeOffset(2026, 4, 12, 16, 30, 0, TimeSpan.Zero);
        var fundAccount = FinancialAccountId.Create();
        var feeAccount = FinancialAccountId.Create();
        var originalPerson = Guid.CreateVersion7();
        var survivingPerson = Guid.CreateVersion7();

        Assert.True(
            FinancialTransaction
                .Create(
                    "USD",
                    now,
                    FinancialTransactionSourceType.Website,
                    FinancialTenderType.CreditCard,
                    FinancialTransactionStatus.Settled,
                    []
                )
                .IsFailure
        );

        var tx = FinancialTransaction
            .Create(
                "USD",
                now,
                FinancialTransactionSourceType.Website,
                FinancialTenderType.CreditCard,
                FinancialTransactionStatus.Settled,
                [
                    (fundAccount, 100.00m, null, true, "Annual Fund"),
                    (feeAccount, 25.00m, null, false, "Event Ticket"),
                ],
                authorizedPersonId: originalPerson,
                transactionCode: "TX-1001"
            )
            .Value;

        Assert.Equal(125.00m, tx.TotalAmount);
        Assert.Equal(2, tx.Details.Count);
        Assert.True(tx.RepointPerson(originalPerson, survivingPerson));
        Assert.Equal(survivingPerson, tx.AuthorizedPersonId);

        _ = tx.AttachEncryptedImage(
            default,
            CheckImageSide.Front,
            "finance/checks/front.enc",
            "image/png",
            512,
            "abc123",
            "nonce",
            "tag",
            "key-v1",
            now
        );
        _ = Assert.Single(tx.Images);

        Assert.True(tx.RecordRefund(25.00m, "", "RF-0", processedByPersonId: null, now).IsFailure);
        Assert.True(
            tx.RecordRefund(200.00m, "Too high", "RF-1", processedByPersonId: null, now).IsFailure
        );

        Assert.True(
            tx.RecordRefund(
                25.00m,
                "Ticket cancelled",
                "RF-2",
                processedByPersonId: null,
                now
            ).IsSuccess
        );
        Assert.Equal(FinancialTransactionStatus.PartiallyRefunded, tx.Status);
        Assert.Equal(100.00m, tx.NetAmount);

        Assert.True(
            tx.RecordRefund(
                100.00m,
                "Full reversal",
                "RF-3",
                processedByPersonId: null,
                now
            ).IsSuccess
        );
        Assert.Equal(FinancialTransactionStatus.Refunded, tx.Status);
        Assert.Equal(0m, tx.NetAmount);
        Assert.True(
            tx.RecordRefund(
                1.00m,
                "Already refunded",
                "RF-4",
                processedByPersonId: null,
                now
            ).IsFailure
        );
    }
}
