using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Recurring;

/// <summary>Recurring charitable gift, membership subscription, or retainer schedule aggregate.</summary>
public sealed class RecurringScheduleTransaction
    : AggregateRoot<RecurringScheduleTransactionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private readonly List<RecurringScheduleSplit> _splits = [];

    private RecurringScheduleTransaction() { }

    private RecurringScheduleTransaction(
        RecurringScheduleTransactionId id,
        Guid personId,
        Guid? householdId,
        FinancialGatewayId financialGatewayId,
        SavedPaymentMethodId savedPaymentMethodId,
        RecurringSchedulePurpose purpose,
        PledgeFrequency frequency,
        string currencyCode,
        decimal totalAmount,
        DateOnly startDate,
        DateOnly? endDate,
        int anchorDayOfMonth
    )
        : base(id)
    {
        PersonId = personId;
        HouseholdId = householdId;
        FinancialGatewayId = financialGatewayId;
        SavedPaymentMethodId = savedPaymentMethodId;
        Purpose = purpose;
        Frequency = frequency;
        CurrencyCode = currencyCode;
        TotalAmount = totalAmount;
        StartDate = startDate;
        EndDate = endDate;
        NextPaymentDate = startDate;
        AnchorDayOfMonth = anchorDayOfMonth;
        Status = RecurringScheduleStatus.Active;
    }

    public Guid PersonId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public FinancialGatewayId FinancialGatewayId { get; private set; }
    public SavedPaymentMethodId SavedPaymentMethodId { get; private set; }
    public string? GatewaySubscriptionReference { get; private set; }
    public RecurringSchedulePurpose Purpose { get; private set; }
    public PledgeFrequency Frequency { get; private set; }
    public RecurringScheduleStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal TotalAmount { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly NextPaymentDate { get; private set; }
    public int AnchorDayOfMonth { get; private set; }
    public DateTimeOffset? LastProcessedAtUtc { get; private set; }
    public int ConsecutiveFailureCount { get; private set; }
    public string? LastFailureReason { get; private set; }

    public IReadOnlyCollection<RecurringScheduleSplit> Splits => _splits.AsReadOnly();

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

    /// <summary>Creates a validated <see cref="RecurringScheduleTransaction" /> aggregate.</summary>
    public static Result<RecurringScheduleTransaction> Create(
        Guid personId,
        FinancialGatewayId gatewayId,
        SavedPaymentMethodId savedPaymentMethodId,
        RecurringSchedulePurpose purpose,
        PledgeFrequency frequency,
        string currencyCode,
        DateOnly startDate,
        DateOnly? endDate,
        IReadOnlyList<(FinancialAccountId AccountId, decimal Amount, string? Summary)> splits,
        Guid? householdId = null
    )
    {
        if (
            HasMissingScheduleActors(personId, gatewayId, savedPaymentMethodId)
            || HasInvalidCadenceWindow(frequency, startDate, endDate)
        )
        {
            return FinanceErrors.RecurringScheduleInvalid;
        }

        if (!IsoCurrencyCatalog.TryGetScale(currencyCode, out var currency, out var scale))
        {
            return FinanceErrors.CurrencyCodeInvalid;
        }

        var id = RecurringScheduleTransactionId.Create();
        var builtSplits = BuildSplitEntities(id, splits, scale, out var total);
        if (builtSplits is null || total <= 0m)
        {
            return FinanceErrors.RecurringScheduleInvalid;
        }

        var schedule = new RecurringScheduleTransaction(
            id,
            personId,
            NormalizeGuid(householdId),
            gatewayId,
            savedPaymentMethodId,
            purpose,
            frequency,
            currency,
            total,
            startDate,
            endDate,
            startDate.Day
        );
        schedule._splits.AddRange(builtSplits);
        return schedule;
    }

    /// <summary>Sets or updates the external gateway subscription reference.</summary>
    public void SetGatewaySubscriptionReference(string? reference)
    {
        GatewaySubscriptionReference = string.IsNullOrWhiteSpace(reference)
            ? null
            : reference.Trim();
    }

    /// <summary>Updates payment method, cadence, next date, and splits for self-service or staff adjustments.</summary>
    public Result UpdateSchedule(
        SavedPaymentMethodId savedPaymentMethodId,
        PledgeFrequency frequency,
        DateOnly nextPaymentDate,
        DateOnly? endDate,
        IReadOnlyList<(FinancialAccountId AccountId, decimal Amount, string? Summary)> splits
    )
    {
        if (
            !CanUpdateSchedule(savedPaymentMethodId)
            || HasInvalidCadenceWindow(frequency, nextPaymentDate, endDate)
        )
        {
            return FinanceErrors.RecurringScheduleInvalid;
        }

        _ = IsoCurrencyCatalog.TryGetScale(CurrencyCode, out _, out var scale);
        var built = BuildSplitEntities(Id, splits, scale, out var total);
        if (built is null || total <= 0m)
        {
            return FinanceErrors.RecurringScheduleInvalid;
        }

        SavedPaymentMethodId = savedPaymentMethodId;
        Frequency = frequency;
        NextPaymentDate = nextPaymentDate;
        AnchorDayOfMonth = nextPaymentDate.Day;
        EndDate = endDate;
        TotalAmount = total;
        _splits.Clear();
        _splits.AddRange(built);
        return Result.Success();
    }

    /// <summary>Advances the schedule after a successful installment charge.</summary>
    public void AdvanceAfterSettlement(DateOnly settledDate, DateTimeOffset processedAtUtc)
    {
        LastProcessedAtUtc = processedAtUtc.ToUniversalTime();
        ConsecutiveFailureCount = 0;
        LastFailureReason = null;

        var next = RecurringDateCalculator.Advance(settledDate, Frequency, AnchorDayOfMonth);
        if (EndDate.HasValue && next > EndDate.Value)
        {
            Status = RecurringScheduleStatus.Completed;
            return;
        }

        NextPaymentDate = next;
        if (Status == RecurringScheduleStatus.PastDue)
        {
            Status = RecurringScheduleStatus.Active;
        }
    }

    /// <summary>Records a failed billing attempt and transitions to PastDue after reaching <paramref name="maxFailuresBeforePastDue" />.</summary>
    public void RecordFailedAttempt(
        string? reason,
        DateTimeOffset attemptedAtUtc,
        int maxFailuresBeforePastDue = 3
    )
    {
        LastProcessedAtUtc = attemptedAtUtc.ToUniversalTime();
        ConsecutiveFailureCount++;
        LastFailureReason = string.IsNullOrWhiteSpace(reason) ? "Payment declined" : reason.Trim();

        if (ConsecutiveFailureCount >= Math.Max(1, maxFailuresBeforePastDue))
        {
            Status = RecurringScheduleStatus.PastDue;
        }
    }

    /// <summary>Pauses an active recurring schedule.</summary>
    public Result Pause()
    {
        if (Status != RecurringScheduleStatus.Active)
        {
            return FinanceErrors.RecurringScheduleStateConflict;
        }

        Status = RecurringScheduleStatus.Paused;
        return Result.Success();
    }

    /// <summary>Resumes a Paused or PastDue recurring schedule.</summary>
    public Result Resume(DateOnly nextPaymentDate)
    {
        if (Status is not (RecurringScheduleStatus.Paused or RecurringScheduleStatus.PastDue))
        {
            return FinanceErrors.RecurringScheduleStateConflict;
        }

        Status = RecurringScheduleStatus.Active;
        ConsecutiveFailureCount = 0;
        LastFailureReason = null;
        NextPaymentDate = nextPaymentDate;
        return Result.Success();
    }

    /// <summary>Cancels a recurring schedule.</summary>
    public Result Cancel(string? reason)
    {
        if (Status == RecurringScheduleStatus.Cancelled)
        {
            return FinanceErrors.RecurringScheduleStateConflict;
        }

        Status = RecurringScheduleStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            LastFailureReason = reason.Trim();
        }

        return Result.Success();
    }

    /// <summary>Repoints <see cref="PersonId" /> when duplicate person records are merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }

    private static List<RecurringScheduleSplit>? BuildSplitEntities(
        RecurringScheduleTransactionId scheduleId,
        IReadOnlyList<(FinancialAccountId AccountId, decimal Amount, string? Summary)> specs,
        int scale,
        out decimal total
    )
    {
        total = 0m;
        if (specs is null || specs.Count == 0)
        {
            return null;
        }

        var list = new List<RecurringScheduleSplit>(specs.Count);
        foreach (var (accountId, amount, summary) in specs)
        {
            if (
                accountId.IsEmpty
                || amount <= 0m
                || !IsoCurrencyCatalog.HasExactScale(amount, scale)
            )
            {
                return null;
            }

            var clean = decimal.Round(amount, scale, MidpointRounding.AwayFromZero);
            total += clean;
            list.Add(
                new RecurringScheduleSplit(
                    RecurringScheduleSplitId.Create(),
                    scheduleId,
                    accountId,
                    clean,
                    string.IsNullOrWhiteSpace(summary) ? null : summary.Trim()
                )
            );
        }

        return list;
    }

    private bool CanUpdateSchedule(SavedPaymentMethodId savedPaymentMethodId)
    {
        return Status
                is not (RecurringScheduleStatus.Cancelled or RecurringScheduleStatus.Completed)
            && !savedPaymentMethodId.IsEmpty;
    }

    private static bool HasMissingScheduleActors(
        Guid personId,
        FinancialGatewayId gatewayId,
        SavedPaymentMethodId savedPaymentMethodId
    )
    {
        return personId == Guid.Empty || gatewayId.IsEmpty || savedPaymentMethodId.IsEmpty;
    }

    private static bool HasInvalidCadenceWindow(
        PledgeFrequency frequency,
        DateOnly startOrNextDate,
        DateOnly? endDate
    )
    {
        return frequency == PledgeFrequency.OneTime
            || (endDate.HasValue && endDate.Value < startOrNextDate);
    }

    private static Guid? NormalizeGuid(Guid? id)
    {
        return id is { } guid && guid != Guid.Empty ? guid : null;
    }
}
