using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Financial transaction aggregate owning multi-account splits, encrypted check image links, and refund history.</summary>
public sealed class FinancialTransaction
    : AggregateRoot<FinancialTransactionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private readonly List<FinancialTransactionDetail> _details = [];
    private readonly List<FinancialTransactionImage> _images = [];
    private readonly List<FinancialTransactionRefund> _refunds = [];

    private FinancialTransaction() { }

    private FinancialTransaction(
        FinancialTransactionId id,
        string? transactionCode,
        Guid? authorizedPersonId,
        Guid? householdId,
        FinancialBatchId? batchId,
        FinancialGatewayId? financialGatewayId,
        SavedPaymentMethodId? savedPaymentMethodId,
        Guid? recurringScheduleId,
        DateTimeOffset transactionDateTimeUtc,
        FinancialTransactionSourceType sourceType,
        FinancialTenderType tenderType,
        FinancialTransactionStatus status,
        string currencyCode,
        decimal totalAmount,
        string? summary,
        string? checkMicrHash
    )
        : base(id)
    {
        TransactionCode = transactionCode;
        AuthorizedPersonId = authorizedPersonId;
        HouseholdId = householdId;
        BatchId = batchId;
        FinancialGatewayId = financialGatewayId;
        SavedPaymentMethodId = savedPaymentMethodId;
        RecurringScheduleId = recurringScheduleId;
        TransactionDateTimeUtc = transactionDateTimeUtc.ToUniversalTime();
        SourceType = sourceType;
        TenderType = tenderType;
        Status = status;
        CurrencyCode = currencyCode;
        TotalAmount = totalAmount;
        Summary = summary;
        CheckMicrHash = checkMicrHash;
    }

    public string? TransactionCode { get; private set; }
    public Guid? AuthorizedPersonId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public FinancialBatchId? BatchId { get; private set; }
    public FinancialGatewayId? FinancialGatewayId { get; private set; }
    public SavedPaymentMethodId? SavedPaymentMethodId { get; private set; }
    public Guid? RecurringScheduleId { get; private set; }
    public DateTimeOffset TransactionDateTimeUtc { get; private set; }
    public FinancialTransactionSourceType SourceType { get; private set; }
    public FinancialTenderType TenderType { get; private set; }
    public FinancialTransactionStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal TotalAmount { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public decimal NetAmount => Math.Max(0m, TotalAmount - RefundedAmount);
    public string? Summary { get; private set; }
    public string? CheckMicrHash { get; private set; }

    public IReadOnlyCollection<FinancialTransactionDetail> Details => _details.AsReadOnly();
    public IReadOnlyCollection<FinancialTransactionImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<FinancialTransactionRefund> Refunds => _refunds.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated <see cref="FinancialTransaction" /> with immutable tax-deductible split snapshots.</summary>
    public static Result<FinancialTransaction> Create(
        string currencyCode,
        DateTimeOffset transactionDateTimeUtc,
        FinancialTransactionSourceType sourceType,
        FinancialTenderType tenderType,
        FinancialTransactionStatus status,
        IReadOnlyList<(
            FinancialAccountId AccountId,
            decimal Amount,
            InvoiceId? InvoiceId,
            bool IsTaxDeductible,
            string? Summary
        )> splits,
        Guid? authorizedPersonId = null,
        Guid? householdId = null,
        FinancialBatchId? batchId = null,
        FinancialGatewayId? financialGatewayId = null,
        SavedPaymentMethodId? savedPaymentMethodId = null,
        Guid? recurringScheduleId = null,
        string? transactionCode = null,
        string? summary = null,
        string? checkMicrHash = null
    )
    {
        if (!IsoCurrencyCatalog.TryGetScale(currencyCode, out var currency, out var scale))
        {
            return FinanceErrors.CurrencyCodeInvalid;
        }

        if (splits is null || splits.Count == 0)
        {
            return FinanceErrors.TransactionSplitsInvalid;
        }

        var id = FinancialTransactionId.Create();
        var builtDetails = BuildDetails(id, splits, scale, out var totalAmount);
        if (builtDetails is null || totalAmount <= 0m)
        {
            return FinanceErrors.TransactionSplitsInvalid;
        }

        var tx = InstantiateTransaction(
            id,
            currency,
            totalAmount,
            transactionDateTimeUtc,
            sourceType,
            tenderType,
            status,
            authorizedPersonId,
            householdId,
            batchId,
            financialGatewayId,
            savedPaymentMethodId,
            recurringScheduleId,
            transactionCode,
            summary,
            checkMicrHash
        );
        tx._details.AddRange(builtDetails);
        return tx;
    }

    private static FinancialTransaction InstantiateTransaction(
        FinancialTransactionId id,
        string currency,
        decimal totalAmount,
        DateTimeOffset transactionDateTimeUtc,
        FinancialTransactionSourceType sourceType,
        FinancialTenderType tenderType,
        FinancialTransactionStatus status,
        Guid? authorizedPersonId,
        Guid? householdId,
        FinancialBatchId? batchId,
        FinancialGatewayId? financialGatewayId,
        SavedPaymentMethodId? savedPaymentMethodId,
        Guid? recurringScheduleId,
        string? transactionCode,
        string? summary,
        string? checkMicrHash
    )
    {
        return new FinancialTransaction(
            id,
            TrimOptional(transactionCode, 80),
            NormalizeGuid(authorizedPersonId),
            NormalizeGuid(householdId),
            NormalizeStrongId(batchId),
            NormalizeStrongId(financialGatewayId),
            NormalizeStrongId(savedPaymentMethodId),
            NormalizeGuid(recurringScheduleId),
            transactionDateTimeUtc,
            sourceType,
            tenderType,
            status,
            currency,
            totalAmount,
            TrimOptional(summary, 500),
            TrimOptional(checkMicrHash, 64)
        );
    }

    private static TId? NormalizeStrongId<TId>(TId? candidate)
        where TId : struct, IStronglyTypedId<TId>
    {
        return candidate is { } value && !value.IsEmpty ? value : null;
    }

    /// <summary>Attaches encrypted check or remittance image metadata to this transaction.</summary>
    public FinancialTransactionImage AttachEncryptedImage(
        FinancialTransactionImageId imageId,
        CheckImageSide imageSide,
        string storageObjectKey,
        string contentType,
        long originalByteLength,
        string sha256PlaintextHex,
        string encryptionNonceBase64,
        string encryptionTagBase64,
        string keyIdentifier,
        DateTimeOffset uploadedOnUtc
    )
    {
        var image = new FinancialTransactionImage(
            imageId.IsEmpty ? FinancialTransactionImageId.Create() : imageId,
            Id,
            imageSide,
            storageObjectKey,
            contentType,
            originalByteLength,
            sha256PlaintextHex,
            encryptionNonceBase64,
            encryptionTagBase64,
            keyIdentifier,
            uploadedOnUtc
        );
        _images.Add(image);
        return image;
    }

    /// <summary>Records a full or partial refund, enforcing cumulative refund bounds.</summary>
    public Result<FinancialTransactionRefund> RecordRefund(
        decimal amount,
        string reason,
        string? gatewayRefundReference,
        Guid? processedByPersonId,
        DateTimeOffset refundedAtUtc
    )
    {
        var validMoney = Money.CreatePositive(amount, CurrencyCode);
        if (validMoney.IsFailure)
        {
            return Result.Failure<FinancialTransactionRefund>(validMoney.Errors);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return DomainError.Validation(
                "Finance.Transaction.RefundReasonRequired",
                "A refund reason is required."
            );
        }

        if (
            Status is FinancialTransactionStatus.Voided or FinancialTransactionStatus.Refunded
            || RefundedAmount + validMoney.Value.Amount > TotalAmount
        )
        {
            return FinanceErrors.RefundExceedsSettledAmount;
        }

        RefundedAmount += validMoney.Value.Amount;
        Status =
            RefundedAmount >= TotalAmount
                ? FinancialTransactionStatus.Refunded
                : FinancialTransactionStatus.PartiallyRefunded;

        var refund = new FinancialTransactionRefund(
            FinancialTransactionRefundId.Create(),
            Id,
            validMoney.Value.Amount,
            reason.Trim(),
            TrimOptional(gatewayRefundReference, 120),
            NormalizeGuid(processedByPersonId),
            refundedAtUtc
        );
        _refunds.Add(refund);
        return refund;
    }

    /// <summary>Repoints <see cref="AuthorizedPersonId" /> when duplicate person records are merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (AuthorizedPersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        AuthorizedPersonId = survivingPersonId;
        return true;
    }

    private static List<FinancialTransactionDetail>? BuildDetails(
        FinancialTransactionId txId,
        IReadOnlyList<(
            FinancialAccountId AccountId,
            decimal Amount,
            InvoiceId? InvoiceId,
            bool IsTaxDeductible,
            string? Summary
        )> splits,
        int scale,
        out decimal sum
    )
    {
        sum = 0m;
        var list = new List<FinancialTransactionDetail>(splits.Count);
        foreach (var (accountId, amount, invoiceId, isTaxDeductible, summary) in splits)
        {
            if (
                accountId.IsEmpty
                || amount <= 0m
                || !IsoCurrencyCatalog.HasExactScale(amount, scale)
            )
            {
                return null;
            }

            var cleanAmount = decimal.Round(amount, scale, MidpointRounding.AwayFromZero);
            sum += cleanAmount;
            list.Add(
                new FinancialTransactionDetail(
                    FinancialTransactionDetailId.Create(),
                    txId,
                    accountId,
                    cleanAmount,
                    invoiceId is { } invId && !invId.IsEmpty ? invId : null,
                    isTaxDeductible,
                    TrimOptional(summary, 300)
                )
            );
        }

        return list;
    }

    private static Guid? NormalizeGuid(Guid? id)
    {
        return id is { } guid && guid != Guid.Empty ? guid : null;
    }

    private static string? TrimOptional(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }
}
