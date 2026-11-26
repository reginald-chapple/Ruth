using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Command to evaluate a personalization segment and refresh its PostgreSQL snapshot and Redis bitset/set cache.</summary>
public sealed record EvaluatePersonalizationSegmentCommand(string SegmentIdOrKey)
    : ICommand<PersonalizationSegmentResponse>;
