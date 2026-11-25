using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

/// <summary>Validated participant eligibility constraints owned by a <see cref="CheckInArea" />.</summary>
public sealed class CheckInEligibilityRule
{
    private const decimal MaxSupportedAgeYears = 130m;
    private const int MinSupportedGrade = -2;
    private const int MaxSupportedGrade = 20;

    private CheckInEligibilityRule() { }

    private CheckInEligibilityRule(
        decimal? minAgeYears,
        decimal? maxAgeYears,
        DateOnly? minBirthDate,
        DateOnly? maxBirthDate,
        int? minGrade,
        int? maxGrade,
        IReadOnlyList<string> allowedMembershipTiers,
        IReadOnlyList<string> requiredSkillOrBadgeCodes,
        bool supportsAccommodations,
        bool requiresAccommodationFlag,
        EligibilityEnrollmentMode enrollmentMode
    )
    {
        MinAgeYears = minAgeYears;
        MaxAgeYears = maxAgeYears;
        MinBirthDate = minBirthDate;
        MaxBirthDate = maxBirthDate;
        MinGrade = minGrade;
        MaxGrade = maxGrade;
        AllowedMembershipTiers = allowedMembershipTiers;
        RequiredSkillOrBadgeCodes = requiredSkillOrBadgeCodes;
        SupportsAccommodations = supportsAccommodations || requiresAccommodationFlag;
        RequiresAccommodationFlag = requiresAccommodationFlag;
        EnrollmentMode = enrollmentMode;
    }

    /// <summary>Gets the optional inclusive minimum age in years.</summary>
    public decimal? MinAgeYears { get; private set; }

    /// <summary>Gets the optional inclusive maximum age in years.</summary>
    public decimal? MaxAgeYears { get; private set; }

    /// <summary>Gets the optional inclusive earliest birth date.</summary>
    public DateOnly? MinBirthDate { get; private set; }

    /// <summary>Gets the optional inclusive latest birth date.</summary>
    public DateOnly? MaxBirthDate { get; private set; }

    /// <summary>Gets the optional inclusive minimum grade level.</summary>
    public int? MinGrade { get; private set; }

    /// <summary>Gets the optional inclusive maximum grade level.</summary>
    public int? MaxGrade { get; private set; }

    /// <summary>Gets the optional allow-list of membership tiers.</summary>
    public IReadOnlyList<string> AllowedMembershipTiers { get; private set; } = [];

    /// <summary>Gets the required skill or badge codes every attendee must possess.</summary>
    public IReadOnlyList<string> RequiredSkillOrBadgeCodes { get; private set; } = [];

    /// <summary>Gets whether this area supports accessibility or sensory accommodations.</summary>
    public bool SupportsAccommodations { get; private set; }

    /// <summary>Gets whether this area is reserved exclusively for participants requesting accommodations.</summary>
    public bool RequiresAccommodationFlag { get; private set; }

    /// <summary>Gets the group enrollment / shift requirement mode.</summary>
    public EligibilityEnrollmentMode EnrollmentMode { get; private set; }

    /// <summary>Validates and constructs an immutable eligibility rule.</summary>
    public static Result<CheckInEligibilityRule> Create(CheckInEligibilityRuleDto? dto)
    {
        var spec = dto ?? new CheckInEligibilityRuleDto();
        var boundsCheck = ValidateScalarBounds(spec);
        if (boundsCheck.IsFailure)
        {
            return Result.Failure<CheckInEligibilityRule>(boundsCheck.Errors);
        }

        var tiers = CheckInTextPolicy.NormalizeTokens(spec.AllowedMembershipTiers);
        var skills = CheckInTextPolicy.NormalizeTokens(spec.RequiredSkillOrBadgeCodes);
        var combined = Result.Combine(tiers, skills);
        return combined.IsFailure
            ? Result.Failure<CheckInEligibilityRule>(combined.Errors)
            : Result.Success(
                new CheckInEligibilityRule(
                    spec.MinAgeYears,
                    spec.MaxAgeYears,
                    spec.MinBirthDate,
                    spec.MaxBirthDate,
                    spec.MinGrade,
                    spec.MaxGrade,
                    tiers.Value,
                    skills.Value,
                    spec.SupportsAccommodations,
                    spec.RequiresAccommodationFlag,
                    spec.EnrollmentMode
                )
            );
    }

    /// <summary>Maps the domain rule back to its contract DTO.</summary>
    public CheckInEligibilityRuleDto ToDto()
    {
        return new CheckInEligibilityRuleDto(
            MinAgeYears,
            MaxAgeYears,
            MinBirthDate,
            MaxBirthDate,
            MinGrade,
            MaxGrade,
            AllowedMembershipTiers,
            RequiredSkillOrBadgeCodes,
            SupportsAccommodations,
            RequiresAccommodationFlag,
            EnrollmentMode
        );
    }

    private static Result ValidateScalarBounds(CheckInEligibilityRuleDto spec)
    {
        var invalidDob =
            spec.MinBirthDate is { } minDob && spec.MaxBirthDate is { } maxDob && minDob > maxDob;
        return !IsValidAgeBounds(spec.MinAgeYears, spec.MaxAgeYears) ? CheckInErrors.InvalidAgeRange
            : invalidDob ? CheckInErrors.InvalidBirthDateRange
            : IsValidGradeBounds(spec.MinGrade, spec.MaxGrade) ? Result.Success()
            : CheckInErrors.InvalidGradeRange;
    }

    private static bool IsValidAgeBounds(decimal? minAge, decimal? maxAge)
    {
        var minOk =
            !minAge.HasValue || (minAge.Value >= 0m && minAge.Value <= MaxSupportedAgeYears);
        var maxOk =
            !maxAge.HasValue || (maxAge.Value >= 0m && maxAge.Value <= MaxSupportedAgeYears);
        var orderOk = !minAge.HasValue || !maxAge.HasValue || minAge.Value <= maxAge.Value;
        return minOk && maxOk && orderOk;
    }

    private static bool IsValidGradeBounds(int? minGrade, int? maxGrade)
    {
        var minOk =
            !minGrade.HasValue
            || (minGrade.Value >= MinSupportedGrade && minGrade.Value <= MaxSupportedGrade);
        var maxOk =
            !maxGrade.HasValue
            || (maxGrade.Value >= MinSupportedGrade && maxGrade.Value <= MaxSupportedGrade);
        var orderOk = !minGrade.HasValue || !maxGrade.HasValue || minGrade.Value <= maxGrade.Value;
        return minOk && maxOk && orderOk;
    }
}
