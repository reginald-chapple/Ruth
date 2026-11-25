using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Evaluates concrete schedule occurrences over a UTC range with category exclusions applied.</summary>
public sealed record GetScheduleOccurrencesQuery(
    Guid ScheduleId,
    DateTimeOffset RangeStartUtc,
    DateTimeOffset RangeEndUtc,
    string? TimeZoneId = null
) : IQuery<IReadOnlyList<ScheduleOccurrenceDto>>;
