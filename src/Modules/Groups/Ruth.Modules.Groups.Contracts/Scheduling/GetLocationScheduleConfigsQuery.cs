using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Lists GroupLocationScheduleConfigs for a Group.</summary>
public sealed record GetLocationScheduleConfigsQuery(Guid GroupId)
    : IQuery<IReadOnlyList<GroupLocationScheduleConfigResponse>>;
