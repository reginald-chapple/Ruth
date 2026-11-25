using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Lists GroupRequirements attached directly to a Group or inherited from its GroupType.</summary>
public sealed record GetGroupRequirementsQuery(Guid? GroupId = null, Guid? GroupTypeId = null)
    : IQuery<IReadOnlyList<GroupRequirementResponse>>;
