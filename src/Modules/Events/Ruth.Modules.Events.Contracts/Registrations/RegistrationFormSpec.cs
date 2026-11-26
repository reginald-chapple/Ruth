namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for a form section within a RegistrationTemplate.</summary>
public sealed record RegistrationFormSpec(
    string Name,
    int DisplayOrder = 0,
    IReadOnlyList<RegistrationFormFieldSpec>? Fields = null
);
