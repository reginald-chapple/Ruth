namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInParticipantContextDto in the CheckIn module.</summary>
public sealed record CheckInParticipantContextDto(
    Guid? PersonId = null,
    decimal? AgeYears = null,
    DateOnly? BirthDate = null,
    int? Grade = null,
    string? MembershipTier = null,
    IReadOnlyList<string>? SkillAndBadgeCodes = null,
    bool NeedsAccommodations = false,
    IReadOnlyList<Guid>? EnrolledGroupIds = null,
    IReadOnlyList<Guid>? ConfirmedShiftGroupIds = null
);
