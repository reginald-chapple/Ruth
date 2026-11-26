using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Transactions;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal static class FinancialTransactionMapper
{
    public static FinancialTransactionResponse ToResponse(
        FinancialTransaction tx,
        IReadOnlyDictionary<FinancialAccountId, string> accountNames
    )
    {
        IReadOnlyList<FinancialTransactionSplitResponse> splits =
        [
            .. tx.Details.Select(d => new FinancialTransactionSplitResponse(
                d.Id.Value,
                d.AccountId.Value,
                accountNames.TryGetValue(d.AccountId, out var name) ? name : "Account",
                d.Amount,
                d.InvoiceId?.Value,
                d.IsTaxDeductible,
                d.Summary
            )),
        ];

        IReadOnlyList<FinancialTransactionImageResponse> images =
        [
            .. tx.Images.Select(ToImageResponse),
        ];

        IReadOnlyList<FinancialTransactionRefundResponse> refunds =
        [
            .. tx.Refunds.Select(static r => new FinancialTransactionRefundResponse(
                r.Id.Value,
                r.Amount,
                r.Reason,
                r.GatewayRefundReference,
                r.ProcessedByPersonId,
                r.RefundedAtUtc
            )),
        ];

        return new FinancialTransactionResponse(
            tx.Id.Value,
            tx.TransactionCode,
            tx.AuthorizedPersonId,
            tx.HouseholdId,
            tx.BatchId?.Value,
            tx.FinancialGatewayId?.Value,
            tx.SavedPaymentMethodId?.Value,
            tx.RecurringScheduleId,
            tx.TransactionDateTimeUtc,
            tx.SourceType,
            tx.TenderType,
            tx.Status,
            tx.CurrencyCode,
            tx.TotalAmount,
            tx.RefundedAmount,
            tx.NetAmount,
            tx.Summary,
            splits,
            images,
            refunds
        );
    }

    public static FinancialTransactionImageResponse ToImageResponse(FinancialTransactionImage img)
    {
        return new FinancialTransactionImageResponse(
            img.Id.Value,
            img.ImageSide,
            img.ContentType,
            img.OriginalByteLength,
            img.Sha256PlaintextHex,
            img.KeyIdentifier,
            img.UploadedOnUtc
        );
    }
}
