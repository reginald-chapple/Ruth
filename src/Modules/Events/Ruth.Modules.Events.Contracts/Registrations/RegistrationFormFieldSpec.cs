namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for a dynamic field within a registration form.</summary>
public sealed record RegistrationFormFieldSpec(
    string FieldKey,
    string Label,
    RegistrationFieldKind FieldKind,
    bool IsRequired = false,
    IReadOnlyList<string>? SelectOptions = null,
    string? ShowWhenFieldKey = null,
    string? ShowWhenEqualsValue = null,
    int DisplayOrder = 0
);
