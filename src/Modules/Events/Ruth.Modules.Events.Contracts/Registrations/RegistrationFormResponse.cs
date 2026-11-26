namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a form section within a RegistrationTemplate.</summary>
public sealed record RegistrationFormResponse(
    Guid Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<RegistrationFormFieldResponse> Fields
);
