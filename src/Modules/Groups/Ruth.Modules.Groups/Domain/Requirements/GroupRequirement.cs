using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Requirements;

/// <summary>Binds a <see cref="GroupRequirementType" /> to either a single Group or an entire GroupType.</summary>
public sealed class GroupRequirement : AggregateRoot<GroupRequirementId>, IAuditableEntity
{
    /// <summary>Canonical entity type key for GroupRequirement.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.GroupRequirement";

    private GroupRequirement() { }

    private GroupRequirement(GroupRequirementId id, GroupRequirementTypeId requirementTypeId)
        : base(id)
    {
        GroupRequirementTypeId = requirementTypeId;
    }

    /// <summary>Gets the optional Group scope (mutually exclusive with <see cref="GroupTypeId" />).</summary>
    public GroupId? GroupId { get; private set; }

    /// <summary>Gets the optional GroupType scope (mutually exclusive with <see cref="GroupId" />).</summary>
    public GroupTypeId? GroupTypeId { get; private set; }

    /// <summary>Gets the referenced GroupRequirementType identifier.</summary>
    public GroupRequirementTypeId GroupRequirementTypeId { get; private set; }

    /// <summary>Gets the optional GroupTypeRole filter (null applies to all roles).</summary>
    public GroupTypeRoleId? GroupRoleId { get; private set; }

    /// <summary>Gets whether the person must satisfy this requirement before being added as a member.</summary>
    public bool MustMeetRequirementToAddMember { get; private set; }

    /// <summary>Gets whether group leaders may override this requirement manually.</summary>
    public bool AllowLeadersToOverride { get; private set; }

    /// <summary>Gets which age classifications this requirement applies to.</summary>
    public RequirementAgeClassification AppliesToAgeClassification { get; private set; }

    /// <summary>Gets the static due date when DueDateType is ConfiguredDate.</summary>
    public DateOnly? DueDateStaticDate { get; private set; }

    /// <summary>Gets the group JSONB attribute key when DueDateType is GroupAttribute.</summary>
    public string? DueDateAttributeKey { get; private set; }

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

    /// <summary>Determines whether this requirement applies to a member with the given role and adult/child flag.</summary>
    public bool AppliesTo(GroupTypeRoleId memberRoleId, bool? isAdult)
    {
        return (!GroupRoleId.HasValue || GroupRoleId.Value == memberRoleId)
            && AppliesToAgeClassification switch
            {
                RequirementAgeClassification.Adults => isAdult != false,
                RequirementAgeClassification.Children => isAdult == false,
                RequirementAgeClassification.All or _ => true,
            };
    }

    internal static Result<GroupRequirement> Create(
        GroupRequirementTypeId requirementTypeId,
        GroupId? groupId,
        GroupTypeId? groupTypeId,
        GroupTypeRoleId? groupRoleId,
        bool mustMeetToAdd,
        bool allowLeadersToOverride,
        RequirementAgeClassification appliesToAgeClassification,
        DateOnly? dueDateStaticDate,
        string? dueDateAttributeKey
    )
    {
        return groupId.HasValue == groupTypeId.HasValue
            ? (Result<GroupRequirement>)GroupsErrors.RequirementInvalidScope
            : Result.Success(
                new GroupRequirement(GroupRequirementId.Create(), requirementTypeId)
                {
                    GroupId = groupId,
                    GroupTypeId = groupTypeId,
                    GroupRoleId = groupRoleId,
                    MustMeetRequirementToAddMember = mustMeetToAdd,
                    AllowLeadersToOverride = allowLeadersToOverride,
                    AppliesToAgeClassification = appliesToAgeClassification,
                    DueDateStaticDate = dueDateStaticDate,
                    DueDateAttributeKey = dueDateAttributeKey?.Trim(),
                }
            );
    }
}
