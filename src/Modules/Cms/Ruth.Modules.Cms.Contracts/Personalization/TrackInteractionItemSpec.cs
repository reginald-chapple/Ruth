namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Single interaction telemetry payload accepted by high-throughput buffered ingestion.</summary>
public sealed record TrackInteractionItemSpec(
    string ChannelKey,
    string ComponentKey,
    string ComponentName,
    string Operation = "View",
    Guid? PersonId = null,
    string? SessionKey = null,
    string? Summary = null,
    string? SourceUrl = null,
    string? DeviceType = null,
    string? Browser = null,
    string? OperatingSystem = null,
    string? IpAddressHash = null,
    Guid? BranchId = null,
    string? ChannelCustom1 = null,
    string? ChannelCustom2 = null,
    DateTimeOffset? OccurredDateTimeUtc = null,
    IReadOnlyDictionary<string, string?>? InteractionData = null
);
