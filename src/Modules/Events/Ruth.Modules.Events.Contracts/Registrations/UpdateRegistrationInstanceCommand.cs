using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to update a RegistrationInstance.</summary>
public sealed record UpdateRegistrationInstanceCommand(
    Guid InstanceId,
    RegistrationInstanceSpec Spec
) : ICommand<RegistrationInstanceResponse>;
