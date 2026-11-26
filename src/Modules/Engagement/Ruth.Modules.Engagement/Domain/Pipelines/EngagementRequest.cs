using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Constituent intake, volunteer onboarding, mentorship, or referral request moving through a PipelineType.</summary>
public sealed class EngagementRequest
    : AggregateRoot<EngagementRequestId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Engagement.EngagementRequest";

    private readonly List<EngagementRequestActivity> _activities = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private EngagementRequest() { }

    private EngagementRequest(EngagementRequestId id)
        : base(id) { }

    public Guid PipelineTypeId { get; private set; }
    public Guid EngagementOpportunityId { get; private set; }
    public Guid RequesterPersonId { get; private set; }
    public Guid? AssignedConnectorPersonId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid CurrentStageId { get; private set; }
    public Guid? PlacementGroupId { get; private set; }
    public Guid? PlacementGroupRoleId { get; private set; }
    public EngagementRequestState RequestState { get; private set; }
    public DateOnly? FollowUpDate { get; private set; }
    public string? Comments { get; private set; }
    public DateTimeOffset LastActivityAtUtc { get; private set; }
    public DateTimeOffset? ConnectedAtUtc { get; private set; }
    public IReadOnlyList<EngagementRequestActivity> Activities => _activities;

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();
    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action) =>
        GlobalDefaultSecured.Instance.IsAllowedByDefault(action);

    public void SetCreated(DateTimeOffset createdOnUtc) =>
        CreatedOnUtc = createdOnUtc.ToUniversalTime();

    public void SetModified(DateTimeOffset modifiedOnUtc) =>
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();

    public void SetConcurrencyStamp(Guid newStamp) => ConcurrencyStamp = newStamp;

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<EngagementRequest> Create(
        PipelineType pipeline,
        EngagementOpportunity opportunity,
        EngagementRequestCreationSpec spec,
        DateOnly today,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(opportunity);
        ArgumentNullException.ThrowIfNull(spec);
        if (
            spec.RequesterPersonId == Guid.Empty
            || opportunity.PipelineTypeId != pipeline.Id.Value
            || pipeline.Stages.Count == 0
        )
        {
            return EngagementErrors.EngagementRequestInvalid;
        }

        if (spec.FollowUpDate.HasValue && (!pipeline.EnableFutureFollowUp || spec.FollowUpDate.Value <= today))
        {
            return EngagementErrors.FutureFollowUpNotAllowed;
        }

        var branch = NormalizeGuid(spec.BranchId) ?? opportunity.DefaultBranchId;
        var connector =
            NormalizeGuid(spec.AssignedConnectorPersonId) ?? opportunity.ResolveDefaultConnector(branch);

        var req = new EngagementRequest(EngagementRequestId.Create())
        {
            PipelineTypeId = pipeline.Id.Value,
            EngagementOpportunityId = opportunity.Id.Value,
            RequesterPersonId = spec.RequesterPersonId,
            AssignedConnectorPersonId = connector,
            BranchId = branch,
            CurrentStageId = pipeline.GetInitialStage().Id.Value,
            PlacementGroupId = NormalizeGuid(spec.PlacementGroupId),
            PlacementGroupRoleId = NormalizeGuid(spec.PlacementGroupRoleId),
            RequestState = spec.FollowUpDate.HasValue
                ? EngagementRequestState.FutureFollowUp
                : EngagementRequestState.Active,
            FollowUpDate = spec.FollowUpDate,
            Comments = TrimOptional(spec.Comments, 2000),
            LastActivityAtUtc = nowUtc.ToUniversalTime(),
        };
        req.AppendActivity(null, "Request Created", connector, spec.Comments, nowUtc);
        return req;
    }

    public void AssignConnector(Guid? connectorPersonId, string? note, DateTimeOffset nowUtc)
    {
        AssignedConnectorPersonId = NormalizeGuid(connectorPersonId);
        LastActivityAtUtc = nowUtc.ToUniversalTime();
        AppendActivity(null, "Assigned Coordinator", AssignedConnectorPersonId, note, nowUtc);
    }

    public Result RecordActivity(
        Guid? activityTypeId,
        string activityName,
        Guid? connectorPersonId,
        string? note,
        DateTimeOffset nowUtc
    )
    {
        if (string.IsNullOrWhiteSpace(activityName))
        {
            return EngagementErrors.EngagementRequestInvalid;
        }

        LastActivityAtUtc = nowUtc.ToUniversalTime();
        AppendActivity(activityTypeId, activityName, connectorPersonId, note, nowUtc);
        return Result.Success();
    }

    public Result<StageTransitionOutcome> TransitionToStage(
        PipelineType pipeline,
        Guid targetStageId,
        Guid? actorPersonId,
        string? note,
        DateOnly today,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        var targetStage = pipeline.FindStage(targetStageId);
        if (targetStage is null || !PipelineBoardEvaluator.IsStageMoveAllowed(pipeline, pipeline.FindStage(CurrentStageId), targetStage))
        {
            return EngagementErrors.InvalidStageTransition;
        }

        PlacementGroupId ??= targetStage
            .Automations.FirstOrDefault(static a => a.AutomationKind == StageAutomationKind.AddToPlacementGroup)
            ?.TargetEntityId;

        if (targetStage.IsTerminalCompleted && pipeline.RequiresPlacementGroup && !PlacementGroupId.HasValue)
        {
            return EngagementErrors.PlacementGroupRequired;
        }

        var prevStageId = CurrentStageId;
        CurrentStageId = targetStage.Id.Value;
        LastActivityAtUtc = nowUtc.ToUniversalTime();
        ApplyStageOutcome(pipeline, targetStage, today, nowUtc);
        AppendActivity(null, $"Moved to {targetStage.Name}", actorPersonId, note, nowUtc);
        return new StageTransitionOutcome(
            prevStageId,
            CurrentStageId,
            RequestState == EngagementRequestState.Connected,
            RequestState == EngagementRequestState.Inactive,
            targetStage.Automations
        );
    }

    public Result ScheduleFollowUp(PipelineType pipeline, DateOnly followUpDate, Guid? actorPersonId, string? note, DateOnly today, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        if (!pipeline.EnableFutureFollowUp || followUpDate <= today)
        {
            return EngagementErrors.FutureFollowUpNotAllowed;
        }

        FollowUpDate = followUpDate;
        RequestState = EngagementRequestState.FutureFollowUp;
        LastActivityAtUtc = nowUtc.ToUniversalTime();
        AppendActivity(null, $"Scheduled Follow-Up ({followUpDate:yyyy-MM-dd})", actorPersonId, note, nowUtc);
        return Result.Success();
    }

    public bool WakeIfDue(DateOnly today, DateTimeOffset nowUtc)
    {
        if (RequestState != EngagementRequestState.FutureFollowUp || !FollowUpDate.HasValue || FollowUpDate.Value > today)
        {
            return false;
        }

        RequestState = EngagementRequestState.Active;
        FollowUpDate = null;
        LastActivityAtUtc = nowUtc.ToUniversalTime();
        AppendActivity(null, "Follow-Up Due — Returned to Active Board", AssignedConnectorPersonId, null, nowUtc);
        return true;
    }

    public Result MarkConnected(PipelineType pipeline, Guid? placementGroupId, Guid? placementGroupRoleId, Guid? actorPersonId, string? note, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        var effectiveGroup = NormalizeGuid(placementGroupId) ?? PlacementGroupId;
        if (pipeline.RequiresPlacementGroup && !effectiveGroup.HasValue)
        {
            return EngagementErrors.PlacementGroupRequired;
        }

        PlacementGroupId = effectiveGroup;
        PlacementGroupRoleId = NormalizeGuid(placementGroupRoleId) ?? PlacementGroupRoleId;
        RequestState = EngagementRequestState.Connected;
        FollowUpDate = null;
        ConnectedAtUtc = nowUtc.ToUniversalTime();
        LastActivityAtUtc = ConnectedAtUtc.Value;
        CurrentStageId = pipeline.Stages.FirstOrDefault(static s => s.IsTerminalCompleted)?.Id.Value ?? CurrentStageId;
        AppendActivity(null, "Marked Connected", actorPersonId, note, nowUtc);
        RaiseConnectedDomainEvent(nowUtc);
        return Result.Success();
    }

    public void MarkInactive(PipelineType? pipeline, string? reason, Guid? actorPersonId, DateTimeOffset nowUtc)
    {
        RequestState = EngagementRequestState.Inactive;
        FollowUpDate = null;
        LastActivityAtUtc = nowUtc.ToUniversalTime();
        CurrentStageId = pipeline?.Stages.FirstOrDefault(static s => s.IsTerminalInactive)?.Id.Value ?? CurrentStageId;
        AppendActivity(null, "Marked Inactive", actorPersonId, reason, nowUtc);
    }

    public void TransferTo(PipelineType newPipeline, EngagementOpportunity newOpportunity, string? note, Guid? actorPersonId, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(newPipeline);
        ArgumentNullException.ThrowIfNull(newOpportunity);
        PipelineTypeId = newPipeline.Id.Value;
        EngagementOpportunityId = newOpportunity.Id.Value;
        CurrentStageId = newPipeline.GetInitialStage().Id.Value;
        AssignedConnectorPersonId = newOpportunity.ResolveDefaultConnector(BranchId);
        RequestState = EngagementRequestState.Active;
        FollowUpDate = null;
        LastActivityAtUtc = nowUtc.ToUniversalTime();
        AppendActivity(null, $"Transferred to {newOpportunity.Name}", actorPersonId, note, nowUtc);
    }

    public bool RepointPerson(Guid retired, Guid surviving)
    {
        var changed = false;
        if (RequesterPersonId == retired)
        {
            RequesterPersonId = surviving;
            changed = true;
        }

        if (AssignedConnectorPersonId == retired)
        {
            AssignedConnectorPersonId = surviving;
            changed = true;
        }

        foreach (var act in _activities)
        {
            changed |= act.RepointConnector(retired, surviving);
        }

        return changed;
    }

    private void ApplyStageOutcome(PipelineType pipeline, PipelineStage stage, DateOnly today, DateTimeOffset nowUtc)
    {
        if (stage.IsTerminalCompleted)
        {
            RequestState = EngagementRequestState.Connected;
            FollowUpDate = null;
            ConnectedAtUtc = nowUtc.ToUniversalTime();
            RaiseConnectedDomainEvent(nowUtc);
            return;
        }

        if (stage.IsTerminalInactive)
        {
            RequestState = EngagementRequestState.Inactive;
            FollowUpDate = null;
            return;
        }

        var autoDays = stage.Automations.FirstOrDefault(static a => a.AutoSetFollowUpDays is > 0)?.AutoSetFollowUpDays;
        if (pipeline.EnableFutureFollowUp && autoDays is > 0)
        {
            FollowUpDate = today.AddDays(autoDays.Value);
            RequestState = EngagementRequestState.FutureFollowUp;
        }
        else if (RequestState is EngagementRequestState.Inactive or EngagementRequestState.Connected)
        {
            RequestState = EngagementRequestState.Active;
        }
    }

    private void RaiseConnectedDomainEvent(DateTimeOffset nowUtc) =>
        RaiseDomainEvent(
            new EngagementRequestConnectedDomainEvent(
                Guid.CreateVersion7(),
                nowUtc.ToUniversalTime(),
                Id.Value,
                PipelineTypeId,
                EngagementOpportunityId,
                RequesterPersonId,
                PlacementGroupId
            )
        );

    private void AppendActivity(Guid? typeId, string name, Guid? connectorId, string? note, DateTimeOffset nowUtc) =>
        _activities.Add(
            new EngagementRequestActivity(
                EngagementRequestActivityId.Create(),
                Id,
                typeId,
                name,
                connectorId,
                TrimOptional(note, 1000),
                nowUtc
            )
        );
}
