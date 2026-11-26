namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a dynamic field within a RegistrationForm.</summary>
public sealed record RegistrationFormFieldResponse(
    Guid Id,
    string FieldKey,
    string Label,
    RegistrationFieldKind FieldKind,
    bool IsRequired,
    IReadOnlyList<string> SelectOptions,
    string? ShowWhenFieldKey,
    string? ShowWhenEqualsValue,
    int DisplayOrder
);
