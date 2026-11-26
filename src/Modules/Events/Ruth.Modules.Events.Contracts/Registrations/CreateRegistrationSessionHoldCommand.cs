using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to acquire a TTL seat-hold RegistrationSession before completing the public wizard.</summary>
public sealed record CreateRegistrationSessionHoldCommand(
    string InstanceIdOrSlug,
    int RequestedSeatCount
) : ICommand<RegistrationSessionResponse>;
