namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Normalized phone number parts ready for persistence and E.164 SMS delivery.</summary>
public readonly record struct NormalizedPhoneNumber(
    string CountryCode,
    string Digits,
    string E164Number,
    string NumberFormatted,
    string NumberReversed
);
