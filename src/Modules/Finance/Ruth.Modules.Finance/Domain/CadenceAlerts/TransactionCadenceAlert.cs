using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.CadenceAlerts;

/// <summary>Detected constituent lapse/churn risk or unusual spike alert instance.</summary>
public sealed class TransactionCadenceAlert
    : AggregateRoot<TransactionCadenceAlertId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private TransactionCadenceAlert() { }

    private TransactionCadenceAlert(
        TransactionCadenceAlertId id,
        TransactionCadenceAlertTypeId alertTypeId,
        CadenceAlertKind alertKind,
        Guid personId,
        decimal baselineMeanAmount,
        decimal baselineIntervalDays,
        decimal observedValue,
        string summary,
        DateTimeOffset detectedOnUtc
    )
        : base(id)
    {
        AlertTypeId = alertTypeId;
        AlertKind = alertKind;
        PersonId = personId;
        BaselineMeanAmount = baselineMeanAmount;
        BaselineIntervalDays = baselineIntervalDays;
        ObservedValue = observedValue;
        Summary = summary;
        DetectedOnUtc = detectedOnUtc.ToUniversalTime();
        Status = CadenceAlertStatus.Open;
    }

    public TransactionCadenceAlertTypeId AlertTypeId { get; private set; }
    public CadenceAlertKind AlertKind { get; private set; }
    public Guid PersonId { get; private set; }
    public CadenceAlertStatus Status { get; private set; }
    public decimal BaselineMeanAmount { get; private set; }
    public decimal BaselineIntervalDays { get; private set; }
    public decimal ObservedValue { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? ResolutionNotes { get; private set; }
    public DateTimeOffset DetectedOnUtc { get; private set; }

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

    /// <summary>Creates a new <see cref="TransactionCadenceAlert" /> from a detector finding.</summary>
    public static TransactionCadenceAlert Create(
        TransactionCadenceAlertTypeId alertTypeId,
        Guid personId,
        CadenceDetectionFinding finding,
        DateTimeOffset detectedOnUtc
    )
    {
        return new TransactionCadenceAlert(
            TransactionCadenceAlertId.Create(),
            alertTypeId,
            finding.Kind,
            personId,
            finding.BaselineMeanAmount,
            finding.BaselineIntervalDays,
            finding.ObservedValue,
            finding.Summary,
            detectedOnUtc
        );
    }

    /// <summary>Transitions the review status and appends optional follow-up notes.</summary>
    public void TransitionStatus(CadenceAlertStatus newStatus, string? notes)
    {
        Status = newStatus;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            var trimmed = notes.Trim();
            ResolutionNotes = trimmed.Length <= 500 ? trimmed : trimmed[..500];
        }
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
}
