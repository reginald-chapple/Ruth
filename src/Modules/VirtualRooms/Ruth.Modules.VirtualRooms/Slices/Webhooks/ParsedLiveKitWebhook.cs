namespace Ruth.Modules.VirtualRooms.Slices.Webhooks;

internal sealed record ParsedLiveKitWebhook(
    string EventType,
    DateTimeOffset OccurredAtUtc,
    string? RoomName,
    string? RoomSid,
    string? ParticipantIdentity,
    Guid? ParticipantPersonId,
    string? ParticipantName,
    string? EgressId,
    string? EgressFilePath,
    long EgressSizeBytes,
    int EgressDurationSeconds,
    string? EgressError
);
