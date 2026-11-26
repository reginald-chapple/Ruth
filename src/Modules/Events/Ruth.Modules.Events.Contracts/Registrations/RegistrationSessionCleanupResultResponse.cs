namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Summary returned after expiring stale TTL RegistrationSessions and promoting waitlisted registrants.</summary>
public sealed record RegistrationSessionCleanupResultResponse(
    int ExpiredSessionCount,
    int AutoPromotedRegistrantCount
);
