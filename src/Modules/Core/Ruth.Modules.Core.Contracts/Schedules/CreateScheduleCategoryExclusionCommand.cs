using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Adds an exclusion date range to a schedule category and refreshes affected schedule dates.</summary>
public sealed record CreateScheduleCategoryExclusionCommand(
    Guid CategoryId,
    string Title,
    DateOnly StartDate,
    DateOnly EndDate
) : ICommand<ScheduleCategoryExclusionResponse>;
