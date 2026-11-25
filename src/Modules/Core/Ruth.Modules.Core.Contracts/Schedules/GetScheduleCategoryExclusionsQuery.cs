using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Lists exclusion date ranges configured for a schedule category.</summary>
public sealed record GetScheduleCategoryExclusionsQuery(Guid CategoryId)
    : IQuery<IReadOnlyList<ScheduleCategoryExclusionResponse>>;
