using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Lists all GroupRequirementTypes.</summary>
public sealed record GetGroupRequirementTypesQuery()
    : IQuery<IReadOnlyList<GroupRequirementTypeResponse>>;
