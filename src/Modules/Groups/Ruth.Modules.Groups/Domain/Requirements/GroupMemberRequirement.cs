using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Requirements;

/// <summary>Persisted evaluation outcome for a specific GroupMember and GroupRequirement.</summary>
public sealed class GroupMemberRequirement
    : AggregateRoot<GroupMemberRequirementId>,
        IAuditableEntity
{
    /// <summary>Canonical entity type key for GroupMemberRequirement.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.GroupMemberRequirement";

    private GroupMemberRequirement() { }

    private GroupMemberRequirement(
        GroupMemberRequirementId id,
        GroupMemberId groupMemberId,
        GroupRequirementId groupRequirementId
    )
        : base(id)
    {
        GroupMemberId = groupMemberId;
        GroupRequirementId = groupRequirementId;
    }

    /// <summary>Gets the GroupMember identifier.</summary>
    public GroupMemberId GroupMemberId { get; private set; }

    /// <summary>Gets the GroupRequirement identifier.</summary>
    public GroupRequirementId GroupRequirementId { get; private set; }

    /// <summary>Gets the calculated requirement state.</summary>
    public MeetsGroupRequirement State { get; private set; } = MeetsGroupRequirement.NotMet;

    /// <summary>Gets when the requirement was met.</summary>
    public DateTimeOffset? RequirementMetOnUtc { get; private set; }

    /// <summary>Gets when the requirement failed.</summary>
    public DateTimeOffset? RequirementFailOnUtc { get; private set; }

    /// <summary>Gets when a warning state was detected.</summary>
    public DateTimeOffset? RequirementWarningOnUtc { get; private set; }

    /// <summary>Gets when the requirement was last checked.</summary>
    public DateTimeOffset? LastCheckedOnUtc { get; private set; }

    /// <summary>Gets the calculated due date for this member.</summary>
    public DateOnly? DueDate { get; private set; }

    /// <summary>Gets whether the requirement was manually completed.</summary>
    public bool WasManuallyCompleted { get; private set; }

    /// <summary>Gets the PersonId who manually completed the requirement.</summary>
    public Guid? ManuallyCompletedByPersonId { get; private set; }

    /// <summary>Gets when manual completion occurred.</summary>
    public DateTimeOffset? ManuallyCompletedOnUtc { get; private set; }

    /// <summary>Gets whether a leader overrode this requirement.</summary>
    public bool WasOverridden { get; private set; }

    /// <summary>Gets the PersonId who overrode the requirement.</summary>
    public Guid? OverriddenByPersonId { get; private set; }

    /// <summary>Gets when the override occurred.</summary>
    public DateTimeOffset? OverriddenOnUtc { get; private set; }

    /// <summary>Gets optional note documenting manual completion or override.</summary>
    public string? OverrideNote { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    internal static GroupMemberRequirement Create(
        GroupMemberId groupMemberId,
        GroupRequirementId groupRequirementId,
        DateOnly? dueDate
    )
    {
        return new GroupMemberRequirement(
            GroupMemberRequirementId.Create(),
            groupMemberId,
            groupRequirementId
        )
        {
            DueDate = dueDate,
        };
    }

    internal void ApplyAutomatedEvaluation(
        MeetsGroupRequirement evaluatedState,
        DateOnly? dueDate,
        DateTimeOffset checkedOnUtc
    )
    {
        var utc = checkedOnUtc.ToUniversalTime();
        LastCheckedOnUtc = utc;
        DueDate = dueDate;

        if (WasOverridden || WasManuallyCompleted)
        {
            State = MeetsGroupRequirement.Meets;
            return;
        }

        State = evaluatedState;
        switch (evaluatedState)
        {
            case MeetsGroupRequirement.Meets:
                RequirementMetOnUtc ??= utc;
                RequirementFailOnUtc = null;
                RequirementWarningOnUtc = null;
                break;
            case MeetsGroupRequirement.MeetsWithWarning:
                RequirementMetOnUtc ??= utc;
                RequirementWarningOnUtc ??= utc;
                RequirementFailOnUtc = null;
                break;
            case MeetsGroupRequirement.NotMet:
                RequirementFailOnUtc = utc;
                RequirementMetOnUtc = null;
                RequirementWarningOnUtc = null;
                break;
            case MeetsGroupRequirement.NotApplicable:
                break;
            case MeetsGroupRequirement.Error:
                break;
            default:
                break;
        }
    }

    internal Result SetManualOrOverride(
        bool isOverride,
        bool allowLeaderOverride,
        bool isMet,
        Guid? actorPersonId,
        string? note,
        DateTimeOffset nowUtc
    )
    {
        if (isOverride && !allowLeaderOverride)
        {
            return GroupsErrors.RequirementOverrideNotAllowed;
        }

        var noteResult = GroupsTextPolicy.NormalizeOptional(note, GroupsTextPolicy.NoteMaxLength);
        if (noteResult.IsFailure)
        {
            return noteResult;
        }

        var utc = nowUtc.ToUniversalTime();
        OverrideNote = noteResult.Value;
        LastCheckedOnUtc = utc;

        if (isOverride)
        {
            WasOverridden = isMet;
            OverriddenByPersonId = isMet ? actorPersonId : null;
            OverriddenOnUtc = isMet ? utc : null;
        }
        else
        {
            WasManuallyCompleted = isMet;
            ManuallyCompletedByPersonId = isMet ? actorPersonId : null;
            ManuallyCompletedOnUtc = isMet ? utc : null;
        }

        RecalculateState(utc);
        return Result.Success();
    }

    internal void RepointActorReferences(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ManuallyCompletedByPersonId == retiredPersonId)
        {
            ManuallyCompletedByPersonId = survivingPersonId;
        }

        if (OverriddenByPersonId == retiredPersonId)
        {
            OverriddenByPersonId = survivingPersonId;
        }
    }

    private void RecalculateState(DateTimeOffset utc)
    {
        if (WasOverridden || WasManuallyCompleted)
        {
            State = MeetsGroupRequirement.Meets;
            RequirementMetOnUtc ??= utc;
            RequirementFailOnUtc = null;
            RequirementWarningOnUtc = null;
            return;
        }

        State = MeetsGroupRequirement.NotMet;
        RequirementMetOnUtc = null;
        RequirementFailOnUtc = utc;
    }
}
