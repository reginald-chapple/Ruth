using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Lists GroupType-level schedule exclusions.</summary>
public sealed record GetGroupScheduleExclusionsQuery(Guid GroupTypeId)
    : IQuery<IReadOnlyList<GroupScheduleExclusionResponse>>;
