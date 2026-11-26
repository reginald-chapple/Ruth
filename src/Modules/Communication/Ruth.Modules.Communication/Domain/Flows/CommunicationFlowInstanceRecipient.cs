using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Individual enrolled recipient progressing through timed steps in a <see cref="CommunicationFlowInstance" />.</summary>
public sealed class CommunicationFlowInstanceRecipient
    : Entity<CommunicationFlowInstanceRecipientId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private Dictionary<string, string?> _mergeFields = new(StringComparer.OrdinalIgnoreCase);

    private CommunicationFlowInstanceRecipient() { }

    private CommunicationFlowInstanceRecipient(
        CommunicationFlowInstanceRecipientId id,
        CommunicationFlowId flowId,
        CommunicationFlowInstanceId instanceId,
        Guid? personId,
        string? emailAddress,
        string? normalizedEmail,
        string? phoneNumber,
        string? displayName,
        DateTimeOffset enrolledAtUtc,
        DateTimeOffset firstStepDueAtUtc,
        Dictionary<string, string?> mergeFields
    )
        : base(id)
    {
        FlowId = flowId;
        FlowInstanceId = instanceId;
        PersonId = personId.HasValue && personId.Value != Guid.Empty ? personId : null;
        EmailAddress = emailAddress;
        NormalizedEmail = normalizedEmail;
        PhoneNumber = phoneNumber;
        DisplayName = displayName;
        Status = FlowRecipientStatus.Active;
        EnrolledAtUtc = enrolledAtUtc.ToUniversalTime();
        NextStepDueAtUtc = firstStepDueAtUtc.ToUniversalTime();
        _mergeFields = mergeFields;
    }

    public CommunicationFlowId FlowId { get; private set; }
    public CommunicationFlowInstanceId FlowInstanceId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string? EmailAddress { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? DisplayName { get; private set; }
    public FlowRecipientStatus Status { get; private set; }
    public int CurrentStepIndex { get; private set; }
    public int CompletedStepsCount { get; private set; }
    public int OpenedStepsCount { get; private set; }
    public int ClickedStepsCount { get; private set; }
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset? LastStepSentAtUtc { get; private set; }
    public DateTimeOffset? NextStepDueAtUtc { get; private set; }
    public DateTimeOffset? ExitedAtUtc { get; private set; }
    public string? ExitReason { get; private set; }

    public IReadOnlyDictionary<string, string?> MergeFields => _mergeFields;

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

    /// <summary>Creates an enrolled recipient positioned at step 0 with its computed first due timestamp.</summary>
    public static Result<CommunicationFlowInstanceRecipient> Enroll(
        CommunicationFlow flow,
        CommunicationFlowInstanceId instanceId,
        Guid? personId,
        string? rawEmail,
        string? rawPhone,
        string? displayName,
        IReadOnlyDictionary<string, string?>? mergeFields,
        DateTimeOffset enrolledAtUtc
    )
    {
        ArgumentNullException.ThrowIfNull(flow);

        var (email, normEmail) = NormalizeOptionalDestination(CommunicationChannel.Email, rawEmail);
        var (phone, _) = NormalizeOptionalDestination(CommunicationChannel.Sms, rawPhone);
        if (email is null && phone is null)
        {
            return Result.Failure<CommunicationFlowInstanceRecipient>(
                CommunicationFlowErrors.FlowEnrollmentEmpty
            );
        }

        var fields = CopyMergeFields(mergeFields);
        var firstDelay = TimeSpan.FromMinutes(flow.Steps[0].DelayMinutes);
        var dueAt = enrolledAtUtc.ToUniversalTime().Add(firstDelay);

        return new CommunicationFlowInstanceRecipient(
            CommunicationFlowInstanceRecipientId.Create(),
            flow.Id,
            instanceId,
            personId,
            email,
            normEmail,
            phone,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            enrolledAtUtc,
            dueAt,
            fields
        );
    }

    /// <summary>Advances the recipient after delivering or skipping the current step.</summary>
    public bool AdvanceToNextStep(
        IReadOnlyList<CommunicationFlowStep> allSteps,
        DateTimeOffset nowUtc,
        bool stepWasSent
    )
    {
        ArgumentNullException.ThrowIfNull(allSteps);
        if (stepWasSent)
        {
            CompletedStepsCount++;
            LastStepSentAtUtc = nowUtc.ToUniversalTime();
        }

        CurrentStepIndex++;
        if (CurrentStepIndex >= allSteps.Count)
        {
            Status = FlowRecipientStatus.Completed;
            NextStepDueAtUtc = null;
            ExitedAtUtc = nowUtc.ToUniversalTime();
            ExitReason = "CompletedAllSteps";
            return true;
        }

        var nextDelay = TimeSpan.FromMinutes(allSteps[CurrentStepIndex].DelayMinutes);
        NextStepDueAtUtc = nowUtc.ToUniversalTime().Add(nextDelay);
        return false;
    }

    /// <summary>Records open/click/conversion signals and applies <see cref="FlowExitCondition" />.</summary>
    public void RecordSignalAndEvaluateExit(
        FlowExitCondition exitCondition,
        bool opened,
        bool clicked,
        bool converted,
        DateTimeOffset nowUtc,
        string? reason = null
    )
    {
        if (opened)
        {
            OpenedStepsCount++;
        }

        if (clicked)
        {
            ClickedStepsCount++;
        }

        if (Status != FlowRecipientStatus.Active)
        {
            return;
        }

        var shouldExit = exitCondition switch
        {
            FlowExitCondition.ExitOnConversion => converted,
            FlowExitCondition.ExitOnConversionOrClick => converted || clicked,
            FlowExitCondition.ExitOnAnyEngagement => converted || clicked || opened,
            FlowExitCondition.RunAllSteps or _ => false,
        };

        if (shouldExit)
        {
            Status = FlowRecipientStatus.ConvertedExited;
            NextStepDueAtUtc = null;
            ExitedAtUtc = nowUtc.ToUniversalTime();
            ExitReason = reason ?? "ExitConditionMet";
        }
    }

    public void MarkUnsubscribedExit(string reason, DateTimeOffset nowUtc)
    {
        Status = FlowRecipientStatus.UnsubscribedExited;
        NextStepDueAtUtc = null;
        ExitedAtUtc = nowUtc.ToUniversalTime();
        ExitReason = reason;
    }

    public void MarkFailed(string reason, DateTimeOffset nowUtc)
    {
        Status = FlowRecipientStatus.Failed;
        NextStepDueAtUtc = null;
        ExitedAtUtc = nowUtc.ToUniversalTime();
        ExitReason = reason;
    }

    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }
    }

    internal void ReplaceMergeFieldsFromPersistence(Dictionary<string, string?> fields)
    {
        _mergeFields = fields;
    }

    private static (string? Display, string? Normalized) NormalizeOptionalDestination(
        CommunicationChannel channel,
        string? raw
    )
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null);
        }

        var res = CommunicationRecipient.NormalizeDestination(channel, raw);
        return res.IsSuccess ? (res.Value.Display, res.Value.Normalized) : (null, null);
    }

    private static Dictionary<string, string?> CopyMergeFields(
        IReadOnlyDictionary<string, string?>? input
    )
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (input is not null)
        {
            foreach (var kvp in input)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    dict[kvp.Key.Trim()] = kvp.Value;
                }
            }
        }

        return dict;
    }
}
