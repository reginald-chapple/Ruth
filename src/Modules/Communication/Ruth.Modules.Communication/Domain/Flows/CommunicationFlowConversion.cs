using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Attributed conversion ledger aggregate for a <see cref="CommunicationFlowInstanceRecipient" />.</summary>
public sealed class CommunicationFlowConversion
    : AggregateRoot<CommunicationFlowConversionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private CommunicationFlowConversion() { }

    private CommunicationFlowConversion(
        CommunicationFlowConversionId id,
        CommunicationFlowId flowId,
        CommunicationFlowInstanceId instanceId,
        CommunicationFlowInstanceRecipientId recipientId,
        Guid? personId,
        FlowConversionGoalKind goalKind,
        int stepOrderAtConversion,
        decimal? conversionValue,
        string? referenceKey,
        string? notes,
        DateTimeOffset occurredAtUtc
    )
        : base(id)
    {
        FlowId = flowId;
        FlowInstanceId = instanceId;
        FlowInstanceRecipientId = recipientId;
        PersonId = personId.HasValue && personId.Value != Guid.Empty ? personId : null;
        GoalKind = goalKind;
        StepOrderAtConversion = Math.Max(0, stepOrderAtConversion);
        ConversionValue = conversionValue;
        ReferenceKey = referenceKey;
        Notes = notes;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }

    public CommunicationFlowId FlowId { get; private set; }
    public CommunicationFlowInstanceId FlowInstanceId { get; private set; }
    public CommunicationFlowInstanceRecipientId FlowInstanceRecipientId { get; private set; }
    public Guid? PersonId { get; private set; }
    public FlowConversionGoalKind GoalKind { get; private set; }
    public int StepOrderAtConversion { get; private set; }
    public decimal? ConversionValue { get; private set; }
    public string? ReferenceKey { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

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

    public static CommunicationFlowConversion Create(
        CommunicationFlowInstanceRecipient recipient,
        FlowConversionGoalKind goalKind,
        decimal? conversionValue,
        string? referenceKey,
        string? notes,
        DateTimeOffset occurredAtUtc
    )
    {
        ArgumentNullException.ThrowIfNull(recipient);
        var effectiveStep = Math.Max(0, recipient.CompletedStepsCount - 1);
        return new CommunicationFlowConversion(
            CommunicationFlowConversionId.Create(),
            recipient.FlowId,
            recipient.FlowInstanceId,
            recipient.Id,
            recipient.PersonId,
            goalKind,
            effectiveStep,
            conversionValue,
            Clean(referenceKey, 300),
            Clean(notes, 500),
            occurredAtUtc
        );
    }

    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }
    }

    private static string? Clean(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }
}
