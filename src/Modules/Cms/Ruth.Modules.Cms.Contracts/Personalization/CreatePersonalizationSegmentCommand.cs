using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Command to create a personalization segment.</summary>
public sealed record CreatePersonalizationSegmentCommand(
    string Name,
    string SegmentKey,
    string? Description = null,
    int TimeToUpdateIntervalMinutes = 60,
    PersonalizationSegmentFilterSpec? FilterSpec = null,
    bool EvaluateImmediately = true
) : ICommand<PersonalizationSegmentResponse>;
