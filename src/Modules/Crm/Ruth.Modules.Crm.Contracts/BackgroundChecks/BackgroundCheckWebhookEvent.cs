namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Normalized event parsed from an inbound background check provider webhook.</summary>
public sealed record BackgroundCheckWebhookEvent(
    string EventId,
    string EventType,
    string ProviderRequestId,
    string? ProviderResponseId,
    string? PackageName,
    BackgroundCheckStatus MappedStatus,
    bool? RecordFound,
    string Recommendation,
    string RawPayloadJson
);
