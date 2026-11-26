namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a TTL seat-hold RegistrationSession.</summary>
public sealed record RegistrationSessionResponse(
    Guid Id,
    Guid InstanceId,
    string SessionToken,
    int RequestedSeatCount,
    int HeldConfirmedSeats,
    int HeldWaitlistSeats,
    RegistrationSessionStatus Status,
    DateTimeOffset ExpiresAtUtc,
    Guid? RegistrationId,
    DateTimeOffset CreatedOnUtc
);
