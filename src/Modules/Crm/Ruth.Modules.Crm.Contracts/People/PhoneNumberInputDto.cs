namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Input payload for creating or updating a person phone number.</summary>
public sealed record PhoneNumberInputDto(
    PhoneNumberType NumberType,
    string Number,
    string? CountryCode = null,
    string? Extension = null,
    bool? IsMessagingEnabled = null,
    bool IsUnlisted = false,
    string? Description = null,
    Guid? NumberTypeValueId = null
);
