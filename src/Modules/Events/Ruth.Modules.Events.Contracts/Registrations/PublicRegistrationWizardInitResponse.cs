namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Public Registration Wizard bootstrap projection combining instance capacity with template forms and fees.</summary>
public sealed record PublicRegistrationWizardInitResponse(
    RegistrationInstanceResponse Instance,
    RegistrationTemplateResponse Template
);
