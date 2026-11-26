using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to update an existing RegistrationTemplate.</summary>
public sealed record UpdateRegistrationTemplateCommand(
    Guid TemplateId,
    RegistrationTemplateSpec Spec
) : ICommand<RegistrationTemplateResponse>;
