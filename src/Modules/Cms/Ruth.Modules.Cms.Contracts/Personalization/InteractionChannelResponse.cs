namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Represents a configured interaction telemetry channel.</summary>
public sealed record InteractionChannelResponse(
    Guid Id,
    string Name,
    string Key,
    InteractionChannelType ChannelType,
    int? RetentionDurationDays,
    bool IsActive,
    int ComponentCount
);
