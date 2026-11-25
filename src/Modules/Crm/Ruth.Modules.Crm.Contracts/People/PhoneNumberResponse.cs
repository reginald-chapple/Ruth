namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Represents a phone number belonging to a person.</summary>
public sealed record PhoneNumberResponse(
    Guid Id,
    PhoneNumberType NumberType,
    Guid? NumberTypeValueId,
    string CountryCode,
    string Number,
    string E164Number,
    string NumberFormatted,
    string? Extension,
    bool IsMessagingEnabled,
    bool IsMessagingOptedOut,
    DateTimeOffset? MessagingOptedOutOnUtc,
    bool IsUnlisted,
    string? Description
);
