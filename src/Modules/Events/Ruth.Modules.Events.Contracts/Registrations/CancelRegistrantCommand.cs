using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to cancel an individual registrant and automatically promote the next FIFO waitlisted registrant when a confirmed seat opens.</summary>
public sealed record CancelRegistrantCommand(Guid RegistrationId, Guid RegistrantId)
    : ICommand<CancelRegistrantResultResponse>;
