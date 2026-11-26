namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Registrant identity, dynamic form field answers, and optional fee selections submitted in a Registration.</summary>
public sealed record RegistrantSubmissionDto(
    string FirstName,
    string LastName,
    string? Email = null,
    string? Phone = null,
    Guid? PersonId = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    IReadOnlyList<RegistrantFeeSelectionInputDto>? FeeSelections = null
);
