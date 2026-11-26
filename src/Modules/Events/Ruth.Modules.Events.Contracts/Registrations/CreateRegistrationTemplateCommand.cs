using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to create a RegistrationTemplate with dynamic forms, fees, discounts, and group placement rules.</summary>
public sealed record CreateRegistrationTemplateCommand(RegistrationTemplateSpec Spec)
    : ICommand<RegistrationTemplateResponse>;
