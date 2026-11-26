using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Command to create an adaptive message campaign.</summary>
public sealed record CreateAdaptiveMessageCommand(
    string Name,
    string Key,
    string? Description = null,
    Guid? CategoryId = null,
    DateTimeOffset? StartDateTimeUtc = null,
    DateTimeOffset? ExpireDateTimeUtc = null,
    bool IsActive = true
) : ICommand<AdaptiveMessageResponse>;
