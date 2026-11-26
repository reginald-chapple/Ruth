namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Represents a persisted interaction telemetry event.</summary>
public sealed record InteractionResponse(
    Guid Id,
    Guid InteractionChannelId,
    string ChannelKey,
    Guid InteractionComponentId,
    string ComponentKey,
    Guid? InteractionSessionId,
    Guid? PersonId,
    string Operation,
    string? Summary,
    string? SourceUrl,
    DateTimeOffset OccurredDateTimeUtc,
    IReadOnlyDictionary<string, string?> InteractionData
);
