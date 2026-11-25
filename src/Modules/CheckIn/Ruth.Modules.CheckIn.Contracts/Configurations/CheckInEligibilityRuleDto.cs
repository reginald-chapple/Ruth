namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInEligibilityRuleDto in the CheckIn module.</summary>
public sealed record CheckInEligibilityRuleDto(
    decimal? MinAgeYears = null,
    decimal? MaxAgeYears = null,
    DateOnly? MinBirthDate = null,
    DateOnly? MaxBirthDate = null,
    int? MinGrade = null,
    int? MaxGrade = null,
    IReadOnlyList<string>? AllowedMembershipTiers = null,
    IReadOnlyList<string>? RequiredSkillOrBadgeCodes = null,
    bool SupportsAccommodations = false,
    bool RequiresAccommodationFlag = false,
    EligibilityEnrollmentMode EnrollmentMode = EligibilityEnrollmentMode.OpenToEligible
);
