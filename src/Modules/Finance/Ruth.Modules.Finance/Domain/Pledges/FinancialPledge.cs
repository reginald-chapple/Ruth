using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Pledges;

/// <summary>Constituent or corporate multi-period financial commitment toward a <see cref="FinancialAccount" />.</summary>
public sealed class FinancialPledge
    : AggregateRoot<FinancialPledgeId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private FinancialPledge() { }

    private FinancialPledge(
        FinancialPledgeId id,
        Guid personId,
        Guid? householdId,
        FinancialAccountId accountId,
        Guid? branchId,
        string currencyCode
    )
        : base(id)
    {
        PersonId = personId;
        HouseholdId = householdId;
        AccountId = accountId;
        BranchId = branchId;
        CurrencyCode = currencyCode;
    }

    public Guid PersonId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public FinancialAccountId AccountId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal TotalAmount { get; private set; }
    public PledgeFrequency Frequency { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FinancialPledgeStatus Status { get; private set; } = FinancialPledgeStatus.Active;
    public string? Notes { get; private set; }

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

    /// <summary>Creates a validated <see cref="FinancialPledge" /> aggregate.</summary>
    public static Result<FinancialPledge> Create(
        Guid personId,
        FinancialAccountId accountId,
        decimal totalAmount,
        string currencyCode,
        PledgeFrequency frequency,
        DateOnly startDate,
        DateOnly endDate,
        Guid? householdId = null,
        Guid? branchId = null,
        string? notes = null
    )
    {
        if (personId == Guid.Empty)
        {
            return FinanceErrors.PledgePersonRequired;
        }

        if (accountId.IsEmpty)
        {
            return FinanceErrors.AccountNotFound;
        }

        var money = Money.CreatePositive(totalAmount, currencyCode);
        if (money.IsFailure)
        {
            return Result.Failure<FinancialPledge>(money.Errors);
        }

        var pledge = new FinancialPledge(
            FinancialPledgeId.Create(),
            personId,
            NormalizeGuid(householdId),
            accountId,
            NormalizeGuid(branchId),
            money.Value.CurrencyCode
        );

        var revised = pledge.Revise(money.Value.Amount, frequency, startDate, endDate, notes);
        return revised.IsFailure ? Result.Failure<FinancialPledge>(revised.Errors) : pledge;
    }

    /// <summary>Updates amount, schedule, or notes on an active pledge.</summary>
    public Result Revise(
        decimal totalAmount,
        PledgeFrequency frequency,
        DateOnly startDate,
        DateOnly endDate,
        string? notes
    )
    {
        if (Status != FinancialPledgeStatus.Active)
        {
            return FinanceErrors.PledgeNotEditable;
        }

        if (endDate < startDate)
        {
            return FinanceErrors.PledgeDateRangeInvalid;
        }

        var money = Money.CreatePositive(totalAmount, CurrencyCode);
        if (money.IsFailure)
        {
            return Result.Failure(money.Errors);
        }

        TotalAmount = money.Value.Amount;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        return Result.Success();
    }

    /// <summary>Marks an active pledge as cancelled.</summary>
    public Result Cancel(string? reason)
    {
        if (Status != FinancialPledgeStatus.Active)
        {
            return FinanceErrors.PledgeNotEditable;
        }

        Status = FinancialPledgeStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var trimmed = reason.Trim();
            Notes = string.IsNullOrWhiteSpace(Notes) ? trimmed : $"{Notes} | Cancelled: {trimmed}";
        }

        return Result.Success();
    }

    /// <summary>Marks the pledge completed when fulfilled.</summary>
    public void MarkCompleted()
    {
        if (Status == FinancialPledgeStatus.Active)
        {
            Status = FinancialPledgeStatus.Completed;
        }
    }

    /// <summary>Computes the number of scheduled installments across the inclusive pledge window.</summary>
    public int CalculateInstallmentCount()
    {
        if (Frequency == PledgeFrequency.OneTime || EndDate <= StartDate)
        {
            return 1;
        }

        var totalDays = EndDate.DayNumber - StartDate.DayNumber;
        var monthsSpan = ((EndDate.Year - StartDate.Year) * 12) + (EndDate.Month - StartDate.Month);

        var count = Frequency switch
        {
            PledgeFrequency.OneTime => 1,
            PledgeFrequency.Weekly => (totalDays / 7) + 1,
            PledgeFrequency.BiWeekly => (totalDays / 14) + 1,
            PledgeFrequency.SemiMonthly => (monthsSpan * 2) + 1,
            PledgeFrequency.Monthly => monthsSpan + 1,
            PledgeFrequency.Quarterly => (monthsSpan / 3) + 1,
            PledgeFrequency.Annually => (monthsSpan / 12) + 1,
            _ => 1,
        };

        return Math.Max(1, count);
    }

    /// <summary>Computes the pro-rata expected contribution amount as of <paramref name="asOfDate" />.</summary>
    public decimal CalculateExpectedByDate(DateOnly asOfDate)
    {
        if (Status == FinancialPledgeStatus.Cancelled || asOfDate < StartDate)
        {
            return 0m;
        }

        if (asOfDate >= EndDate || Frequency == PledgeFrequency.OneTime || EndDate == StartDate)
        {
            return TotalAmount;
        }

        var totalSpanDays = EndDate.DayNumber - StartDate.DayNumber + 1;
        var elapsedDays = asOfDate.DayNumber - StartDate.DayNumber + 1;
        _ = IsoCurrencyCatalog.TryGetScale(CurrencyCode, out _, out var scale);
        var ratio = (decimal)elapsedDays / totalSpanDays;
        return decimal.Round(TotalAmount * ratio, scale, MidpointRounding.AwayFromZero);
    }

    /// <summary>Repoints <see cref="PersonId" /> when a duplicate person record is merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }

    private static Guid? NormalizeGuid(Guid? id)
    {
        return id is { } guid && guid != Guid.Empty ? guid : null;
    }
}
