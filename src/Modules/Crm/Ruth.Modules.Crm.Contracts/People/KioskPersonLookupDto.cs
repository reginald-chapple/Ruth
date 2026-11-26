namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Constituent profile snapshot optimized for check-in eligibility and safety badge rendering.</summary>
/// <param name="PersonId">Person identifier.</param>
/// <param name="FirstName">Legal first name.</param>
/// <param name="NickName">Preferred nickname.</param>
/// <param name="LastName">Last name.</param>
/// <param name="FullName">Formatted full display name.</param>
/// <param name="AgeYears">Fractional or integer age in years when birth date is known.</param>
/// <param name="BirthDate">Exact birth date when known.</param>
/// <param name="Grade">Numeric school grade (0 = Kindergarten, 1..12 = Grades 1..12) when graduation year is known.</param>
/// <param name="GradeFormatted">Formatted grade label.</param>
/// <param name="PrimaryPhoneFormatted">Formatted primary phone number.</param>
/// <param name="IsChildInHousehold">Whether the member has a Child role or child classification.</param>
/// <param name="MembershipTier">Optional membership tier attribute value.</param>
/// <param name="SkillAndBadgeCodes">Normalized skill/certification codes from person attributes.</param>
/// <param name="NeedsAccommodations">Whether accommodation support is flagged on the person profile.</param>
/// <param name="MedicalOrAllergyNote">Optional allergy/medical note from person attributes.</param>
/// <param name="ActiveSignalNotes">Active safety/compliance signal banners attached to the person.</param>
public sealed record KioskPersonLookupDto(
    Guid PersonId,
    string FirstName,
    string NickName,
    string LastName,
    string FullName,
    decimal? AgeYears,
    DateOnly? BirthDate,
    int? Grade,
    string? GradeFormatted,
    string? PrimaryPhoneFormatted,
    bool IsChildInHousehold,
    string? MembershipTier,
    IReadOnlyList<string> SkillAndBadgeCodes,
    bool NeedsAccommodations,
    string? MedicalOrAllergyNote,
    IReadOnlyList<string> ActiveSignalNotes
);
