using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Finance.Domain.Batches;

/// <summary>Financial settlement batch aggregate enforcing Pending to Open to Closed reconciliation and audit locks.</summary>
public sealed class FinancialBatch
    : AggregateRoot<FinancialBatchId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for ACL evaluation.</summary>
    public const string EntityTypeKeyName = "Ruth.Finance.FinancialBatch";

    private FinancialBatch() { }

    private FinancialBatch(
        FinancialBatchId id,
        string name,
        string currencyCode,
        decimal controlAmount,
        int controlItemCount,
        DateTimeOffset batchStartUtc,
        Guid? branchId,
        string? accountingSystemCode,
        bool isAutomated
    )
        : base(id)
    {
        Name = name;
        CurrencyCode = currencyCode;
        ControlAmount = controlAmount;
        ControlItemCount = controlItemCount;
        BatchStartDateTimeUtc = batchStartUtc.ToUniversalTime();
        BranchId = branchId;
        AccountingSystemCode = accountingSystemCode;
        IsAutomated = isAutomated;
        Status = FinancialBatchStatus.Pending;
    }

    public string Name { get; private set; } = string.Empty;
    public FinancialBatchStatus Status { get; private set; }
    public Guid? BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal ControlAmount { get; private set; }
    public int ControlItemCount { get; private set; }
    public DateTimeOffset BatchStartDateTimeUtc { get; private set; }
    public DateTimeOffset? BatchEndDateTimeUtc { get; private set; }
    public string? AccountingSystemCode { get; private set; }
    public bool IsAutomated { get; private set; }
    public Guid? ClosedByPersonId { get; private set; }
    public DateTimeOffset? ClosedOnUtc { get; private set; }
    public string? AuditLockNote { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

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

    /// <summary>Creates a validated <see cref="FinancialBatch" /> aggregate.</summary>
    public static Result<FinancialBatch> Create(
        string name,
        string currencyCode,
        decimal controlAmount,
        int controlItemCount,
        DateTimeOffset batchStartUtc,
        Guid? branchId = null,
        string? accountingSystemCode = null,
        bool isAutomated = false,
        bool openImmediately = false
    )
    {
        var headerMoney = ValidateBatchHeader(name, currencyCode, controlAmount, controlItemCount);
        if (headerMoney.IsFailure)
        {
            return Result.Failure<FinancialBatch>(headerMoney.Errors);
        }

        var batch = new FinancialBatch(
            FinancialBatchId.Create(),
            name.Trim(),
            headerMoney.Value.CurrencyCode,
            headerMoney.Value.Amount,
            controlItemCount,
            batchStartUtc,
            NormalizeGuid(branchId),
            TrimOptional(accountingSystemCode),
            isAutomated
        );

        if (openImmediately)
        {
            _ = batch.Open(controlAmount: null, controlItemCount: null);
        }

        return batch;
    }

    /// <summary>Transitions a Pending batch to Open or updates control targets on an Open batch.</summary>
    public Result Open(decimal? controlAmount, int? controlItemCount)
    {
        if (Status == FinancialBatchStatus.Closed)
        {
            return FinanceErrors.BatchAuditLocked;
        }

        var amountResult = ApplyOptionalControlAmount(controlAmount);
        if (amountResult.IsFailure)
        {
            return amountResult;
        }

        var countResult = ApplyOptionalControlCount(controlItemCount);
        if (countResult.IsFailure)
        {
            return countResult;
        }

        Status = FinancialBatchStatus.Open;
        return Result.Success();
    }

    /// <summary>Increments control totals on an automated open batch (gateway or scanner auto-batching).</summary>
    public Result IncrementAutomatedControl(decimal amountDelta, int countDelta = 1)
    {
        if (Status == FinancialBatchStatus.Closed)
        {
            return FinanceErrors.BatchAuditLocked;
        }

        ControlAmount = Math.Max(0m, ControlAmount + amountDelta);
        if (ControlItemCount > 0 || IsAutomated)
        {
            ControlItemCount = Math.Max(0, ControlItemCount + countDelta);
        }

        return Result.Success();
    }

    /// <summary>Verifies that this batch is Open and unlocked for transaction mutations.</summary>
    public Result EnsureCanPostTransactions()
    {
        return Status == FinancialBatchStatus.Closed ? FinanceErrors.BatchAuditLocked
            : Status != FinancialBatchStatus.Open ? FinanceErrors.BatchInvalidTransition
            : Result.Success();
    }

    /// <summary>Reconciles and closes an Open batch, enforcing zero control variance.</summary>
    public Result Close(
        decimal actualTransactionTotal,
        int actualTransactionCount,
        Guid? closedByPersonId,
        DateTimeOffset closedAtUtc,
        string? auditNote = null
    )
    {
        if (Status != FinancialBatchStatus.Open)
        {
            return FinanceErrors.BatchInvalidTransition;
        }

        if (HasControlVariance(actualTransactionTotal, actualTransactionCount))
        {
            return FinanceErrors.BatchControlVariance;
        }

        RecordClosureMetadata(closedByPersonId, closedAtUtc, auditNote);
        return Result.Success();
    }

    /// <summary>Explicitly reopens a Closed batch when an audit reason is supplied.</summary>
    public Result Reopen(Guid? reopenedByPersonId, string? auditReason)
    {
        if (Status != FinancialBatchStatus.Closed)
        {
            return FinanceErrors.BatchInvalidTransition;
        }

        if (string.IsNullOrWhiteSpace(auditReason))
        {
            return FinanceErrors.BatchAuditLocked;
        }

        Status = FinancialBatchStatus.Open;
        ClosedOnUtc = null;
        ClosedByPersonId = null;
        var actor =
            reopenedByPersonId is { } pid && pid != Guid.Empty ? pid.ToString("D") : "Staff";
        AppendAuditNote($"Reopened by {actor}: {auditReason.Trim()}");
        return Result.Success();
    }

    private void AppendAuditNote(string entry)
    {
        var combined = string.IsNullOrWhiteSpace(AuditLockNote)
            ? entry
            : $"{AuditLockNote} | {entry}";
        AuditLockNote = combined.Length <= 1000 ? combined : combined[^1000..];
    }

    private static Result<Money> ValidateBatchHeader(
        string? name,
        string currencyCode,
        decimal controlAmount,
        int controlItemCount
    )
    {
        return string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160
                ? FinanceErrors.BatchNameInvalid
            : controlItemCount < 0 ? FinanceErrors.AmountMustBeNonNegative
            : Money.CreateNonNegative(controlAmount, currencyCode);
    }

    private Result ApplyOptionalControlAmount(decimal? controlAmount)
    {
        if (controlAmount is not { } updatedAmt)
        {
            return Result.Success();
        }

        var money = Money.CreateNonNegative(updatedAmt, CurrencyCode);
        if (money.IsFailure)
        {
            return Result.Failure(money.Errors);
        }

        ControlAmount = money.Value.Amount;
        return Result.Success();
    }

    private Result ApplyOptionalControlCount(int? controlItemCount)
    {
        if (controlItemCount is not { } updatedCount)
        {
            return Result.Success();
        }

        if (updatedCount < 0)
        {
            return FinanceErrors.AmountMustBeNonNegative;
        }

        ControlItemCount = updatedCount;
        return Result.Success();
    }

    private bool HasControlVariance(decimal actualTransactionTotal, int actualTransactionCount)
    {
        var countMatches = ControlItemCount == 0 || actualTransactionCount == ControlItemCount;
        return actualTransactionTotal != ControlAmount || !countMatches;
    }

    private void RecordClosureMetadata(
        Guid? closedByPersonId,
        DateTimeOffset closedAtUtc,
        string? auditNote
    )
    {
        Status = FinancialBatchStatus.Closed;
        ClosedByPersonId = NormalizeGuid(closedByPersonId);
        ClosedOnUtc = closedAtUtc.ToUniversalTime();
        BatchEndDateTimeUtc ??= ClosedOnUtc;
        if (!string.IsNullOrWhiteSpace(auditNote))
        {
            AppendAuditNote(auditNote.Trim());
        }
    }

    private static Guid? NormalizeGuid(Guid? id)
    {
        return id is { } guid && guid != Guid.Empty ? guid : null;
    }

    private static string? TrimOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
