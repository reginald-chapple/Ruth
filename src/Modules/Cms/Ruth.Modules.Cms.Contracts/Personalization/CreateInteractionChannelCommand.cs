using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Command to create an interaction telemetry channel.</summary>
public sealed record CreateInteractionChannelCommand(
    string Name,
    string Key,
    InteractionChannelType ChannelType = InteractionChannelType.Website,
    int? RetentionDurationDays = 365
) : ICommand<InteractionChannelResponse>;
