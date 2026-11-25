using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Lists schedules with optional category, active, and search filters.</summary>
public sealed record GetSchedulesQuery(
    Guid? CategoryId = null,
    bool ActiveOnly = false,
    string? SearchTerm = null
) : IQuery<IReadOnlyList<ScheduleResponse>>;
