using Ruth.Modules.CheckIn.Contracts.Configurations;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

/// <summary>Pure domain specification evaluator checking participant eligibility against a <see cref="CheckInArea" />.</summary>
public static class CheckInEligibilityEvaluator
{
    /// <summary>Evaluates whether <paramref name="participant" /> satisfies all constraints on <paramref name="area" />.</summary>
    public static (bool IsEligible, IReadOnlyList<string> Reasons) Evaluate(
        CheckInArea area,
        CheckInParticipantContextDto participant
    )
    {
        ArgumentNullException.ThrowIfNull(area);
        ArgumentNullException.ThrowIfNull(participant);

        var reasons = new List<string>(6);
        if (!area.IsActive)
        {
            reasons.Add("AreaInactive");
            return (false, reasons);
        }

        var rule = area.Eligibility;
        EvaluateEnrollmentConstraint(area.LinkedGroupId, rule.EnrollmentMode, participant, reasons);
        EvaluateAgeConstraint(rule, participant.AgeYears, reasons);
        EvaluateBirthDateConstraint(rule, participant.BirthDate, reasons);
        EvaluateGradeConstraint(rule, participant.Grade, reasons);
        EvaluateTierAndSkillConstraints(rule, participant, reasons);

        if (rule.RequiresAccommodationFlag && !participant.NeedsAccommodations)
        {
            reasons.Add("AccommodationRequired");
        }

        return (reasons.Count == 0, reasons);
    }

    private static void EvaluateEnrollmentConstraint(
        Guid? linkedGroupId,
        EligibilityEnrollmentMode mode,
        CheckInParticipantContextDto participant,
        List<string> reasons
    )
    {
        if (mode == EligibilityEnrollmentMode.OpenToEligible)
        {
            return;
        }

        if (!linkedGroupId.HasValue)
        {
            reasons.Add("MissingLinkedGroup");
            return;
        }

        var enrolledSet =
            mode == EligibilityEnrollmentMode.PreEnrolledMembersOnly
                ? participant.EnrolledGroupIds
                : participant.ConfirmedShiftGroupIds;

        if (enrolledSet is null || !enrolledSet.Contains(linkedGroupId.Value))
        {
            reasons.Add(
                mode == EligibilityEnrollmentMode.PreEnrolledMembersOnly
                    ? "NotPreEnrolledInGroup"
                    : "NoConfirmedShiftAssignment"
            );
        }
    }

    private static void EvaluateAgeConstraint(
        CheckInEligibilityRule rule,
        decimal? ageYears,
        List<string> reasons
    )
    {
        if (!rule.MinAgeYears.HasValue && !rule.MaxAgeYears.HasValue)
        {
            return;
        }

        if (!ageYears.HasValue)
        {
            reasons.Add("AgeMissing");
            return;
        }

        var belowMin = rule.MinAgeYears is { } min && ageYears.Value < min;
        var aboveMax = rule.MaxAgeYears is { } max && ageYears.Value > max;
        if (belowMin || aboveMax)
        {
            reasons.Add("AgeOutOfRange");
        }
    }

    private static void EvaluateBirthDateConstraint(
        CheckInEligibilityRule rule,
        DateOnly? birthDate,
        List<string> reasons
    )
    {
        if (!rule.MinBirthDate.HasValue && !rule.MaxBirthDate.HasValue)
        {
            return;
        }

        if (!birthDate.HasValue)
        {
            reasons.Add("BirthDateMissing");
            return;
        }

        var beforeMin = rule.MinBirthDate is { } min && birthDate.Value < min;
        var afterMax = rule.MaxBirthDate is { } max && birthDate.Value > max;
        if (beforeMin || afterMax)
        {
            reasons.Add("BirthDateOutOfRange");
        }
    }

    private static void EvaluateGradeConstraint(
        CheckInEligibilityRule rule,
        int? grade,
        List<string> reasons
    )
    {
        if (!rule.MinGrade.HasValue && !rule.MaxGrade.HasValue)
        {
            return;
        }

        if (!grade.HasValue)
        {
            reasons.Add("GradeMissing");
            return;
        }

        var belowMin = rule.MinGrade is { } min && grade.Value < min;
        var aboveMax = rule.MaxGrade is { } max && grade.Value > max;
        if (belowMin || aboveMax)
        {
            reasons.Add("GradeOutOfRange");
        }
    }

    private static void EvaluateTierAndSkillConstraints(
        CheckInEligibilityRule rule,
        CheckInParticipantContextDto participant,
        List<string> reasons
    )
    {
        if (
            rule.AllowedMembershipTiers.Count > 0
            && !MatchesAnyTier(rule, participant.MembershipTier)
        )
        {
            reasons.Add("MembershipTierNotAllowed");
        }

        if (
            rule.RequiredSkillOrBadgeCodes.Count > 0
            && !PossessesAllSkills(rule.RequiredSkillOrBadgeCodes, participant.SkillAndBadgeCodes)
        )
        {
            reasons.Add("MissingRequiredSkillOrBadge");
        }
    }

    private static bool MatchesAnyTier(CheckInEligibilityRule rule, string? tier)
    {
        return !string.IsNullOrWhiteSpace(tier)
            && rule.AllowedMembershipTiers.Contains(tier.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool PossessesAllSkills(
        IReadOnlyList<string> requiredCodes,
        IReadOnlyList<string>? heldCodes
    )
    {
        if (heldCodes is null || heldCodes.Count == 0)
        {
            return false;
        }

        var held = new HashSet<string>(
            heldCodes.Where(static c => !string.IsNullOrWhiteSpace(c)).Select(static c => c.Trim()),
            StringComparer.OrdinalIgnoreCase
        );
        return requiredCodes.All(held.Contains);
    }
}
