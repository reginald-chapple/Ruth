using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Lists GroupDemographicTypes defined for a GroupType.</summary>
public sealed record GetGroupDemographicTypesQuery(Guid? GroupTypeId = null)
    : IQuery<IReadOnlyList<GroupDemographicTypeResponse>>;
