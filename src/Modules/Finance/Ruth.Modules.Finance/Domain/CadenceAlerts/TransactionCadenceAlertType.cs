using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.CadenceAlerts;

/// <summary>Configurable statistical threshold definition for detecting constituent lapse/churn risk and unusual spikes.</summary>
public sealed class TransactionCadenceAlertType
    : AggregateRoot<TransactionCadenceAlertTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private TransactionCadenceAlertType() { }

    private TransactionCadenceAlertType(TransactionCadenceAlertTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public CadenceAlertKind AlertKind { get; private set; }
    public int MinHistoricalSamples { get; private set; } = 3;
    public decimal MultiplierThreshold { get; private set; } = 2.0m;
    public decimal StandardDeviationSensitivity { get; private set; } = 1.5m;
    public decimal MinimumAmountThreshold { get; private set; } = 50m;
    public int CooldownDays { get; private set; } = 30;
    public FinancialAccountId? AccountId { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    /// <summary>Creates a validated <see cref="TransactionCadenceAlertType" /> rule.</summary>
    public static Result<TransactionCadenceAlertType> Create(TransactionCadenceAlertTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new TransactionCadenceAlertType(TransactionCadenceAlertTypeId.Create());
        var updated = entity.Update(spec);
        return updated.IsFailure
            ? Result.Failure<TransactionCadenceAlertType>(updated.Errors)
            : entity;
    }

    /// <summary>Updates threshold configuration on this cadence alert type.</summary>
    public Result Update(TransactionCadenceAlertTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        if (
            string.IsNullOrWhiteSpace(spec.Name)
            || spec.Name.Trim().Length > 120
            || spec.MinHistoricalSamples < 2
            || spec.MultiplierThreshold <= 1.0m
            || spec.StandardDeviationSensitivity < 0m
            || spec.MinimumAmountThreshold < 0m
            || spec.CooldownDays < 1
        )
        {
            return FinanceErrors.CadenceAlertTypeInvalid;
        }

        Name = spec.Name.Trim();
        AlertKind = spec.AlertKind;
        MinHistoricalSamples = spec.MinHistoricalSamples;
        MultiplierThreshold = spec.MultiplierThreshold;
        StandardDeviationSensitivity = spec.StandardDeviationSensitivity;
        MinimumAmountThreshold = spec.MinimumAmountThreshold;
        CooldownDays = spec.CooldownDays;
        AccountId =
            spec.AccountId is { } acc && acc != Guid.Empty ? FinancialAccountId.From(acc) : null;
        IsActive = spec.IsActive;
        return Result.Success();
    }
}
