using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to create a RegistrationInstance from a RegistrationTemplate.</summary>
public sealed record CreateRegistrationInstanceCommand(RegistrationInstanceSpec Spec)
    : ICommand<RegistrationInstanceResponse>;
