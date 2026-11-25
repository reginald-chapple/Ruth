using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Defines a demographic category tracked across groups of a given GroupType.</summary>
public sealed class GroupDemographicType : AggregateRoot<GroupDemographicTypeId>, IAuditableEntity
{
    private GroupDemographicType() { }

    private GroupDemographicType(GroupDemographicTypeId id, GroupTypeId groupTypeId)
        : base(id)
    {
        GroupTypeId = groupTypeId;
    }

    /// <summary>Gets the owning GroupType identifier.</summary>
    public GroupTypeId GroupTypeId { get; private set; }

    /// <summary>Gets the demographic type display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the logical calculator/component key.</summary>
    public string ComponentKey { get; private set; } = string.Empty;

    /// <summary>Gets whether values are populated automatically.</summary>
    public bool IsAutomated { get; private set; }

    /// <summary>Gets whether automated calculation runs on person updates.</summary>
    public bool RunOnPersonUpdate { get; private set; }

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

    internal static Result<GroupDemographicType> Create(
        GroupTypeId groupTypeId,
        string name,
        string componentKey,
        string? description = null,
        bool isAutomated = false,
        bool runOnPersonUpdate = false
    )
    {
        var nameResult = GroupsTextPolicy.NormalizeRequired(name);
        var keyResult = GroupsTextPolicy.NormalizeRequired(componentKey);
        var descResult = GroupsTextPolicy.NormalizeOptional(description);
        var combined = Result.Combine(nameResult, keyResult, descResult);
        return combined.IsFailure
            ? Result.Failure<GroupDemographicType>(combined.Errors)
            : Result.Success(
                new GroupDemographicType(GroupDemographicTypeId.Create(), groupTypeId)
                {
                    Name = nameResult.Value,
                    ComponentKey = keyResult.Value,
                    Description = descResult.Value,
                    IsAutomated = isAutomated,
                    RunOnPersonUpdate = runOnPersonUpdate,
                }
            );
    }
}
