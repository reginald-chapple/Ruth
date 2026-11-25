namespace Ruth.Modules.Crm.Contracts.Verification;

/// <summary>Represents a recorded person profile view audit entry.</summary>
public sealed record PersonViewedResponse(
    Guid Id,
    Guid TargetPersonId,
    Guid? ViewerPersonId,
    Guid? ViewerUserId,
    DateTimeOffset ViewedOnUtc,
    string? IpAddress,
    string? Source
);
