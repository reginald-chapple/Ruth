namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Outcome of cancelling a registrant, including any FIFO waitlisted registrants automatically promoted.</summary>
public sealed record CancelRegistrantResultResponse(
    RegistrationResponse UpdatedRegistration,
    IReadOnlyList<Guid> AutoPromotedRegistrantIds
);
