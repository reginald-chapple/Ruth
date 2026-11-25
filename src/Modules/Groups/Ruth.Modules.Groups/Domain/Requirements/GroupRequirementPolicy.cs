using System.Globalization;
using Ruth.Modules.Groups.Contracts.Requirements;

namespace Ruth.Modules.Groups.Domain.Requirements;

/// <summary>Pure domain evaluator computing requirement status, expiration windows, and due dates.</summary>
internal static class GroupRequirementPolicy
{
    /// <summary>Evaluates expiration and warning state for a dated evidence timestamp (e.g. clear background check).</summary>
    public static MeetsGroupRequirement EvaluateTimedEvidence(
        GroupRequirementType type,
        DateTimeOffset? completedOnUtc,
        DateTimeOffset nowUtc
    )
    {
        if (!completedOnUtc.HasValue)
        {
            return MeetsGroupRequirement.NotMet;
        }

        if (!type.CanExpire || !type.ExpireInDays.HasValue)
        {
            return MeetsGroupRequirement.Meets;
        }

        var expiresAt = completedOnUtc.Value.ToUniversalTime().AddDays(type.ExpireInDays.Value);
        var utcNow = nowUtc.ToUniversalTime();
        if (utcNow >= expiresAt)
        {
            return MeetsGroupRequirement.NotMet;
        }

        var warningDays = type.WarningDaysBeforeExpiration ?? 30;
        return warningDays > 0 && utcNow >= expiresAt.AddDays(-warningDays)
            ? MeetsGroupRequirement.MeetsWithWarning
            : MeetsGroupRequirement.Meets;
    }

    /// <summary>Computes the effective due date for a GroupMemberRequirement.</summary>
    public static DateOnly? ComputeDueDate(
        GroupRequirementType type,
        GroupRequirement requirement,
        DateTimeOffset? memberAddedOnUtc,
        IReadOnlyDictionary<string, string?> groupAttributes,
        DateTimeOffset nowUtc
    )
    {
        var offset = type.DueDateOffsetInDays ?? 0;
        return type.DueDateType switch
        {
            RequirementDueDateType.Immediate => DateOnly.FromDateTime(
                (memberAddedOnUtc ?? nowUtc).UtcDateTime
            ),
            RequirementDueDateType.ConfiguredDate => requirement.DueDateStaticDate,
            RequirementDueDateType.DaysAfterJoining => DateOnly
                .FromDateTime((memberAddedOnUtc ?? nowUtc).UtcDateTime)
                .AddDays(offset),
            RequirementDueDateType.GroupAttribute => ResolveAttributeDueDate(
                requirement.DueDateAttributeKey,
                groupAttributes,
                offset
            ),
            _ => null,
        };
    }

    private static DateOnly? ResolveAttributeDueDate(
        string? attributeKey,
        IReadOnlyDictionary<string, string?> groupAttributes,
        int offsetDays
    )
    {
        return string.IsNullOrWhiteSpace(attributeKey)
            || !groupAttributes.TryGetValue(attributeKey, out var raw)
            || string.IsNullOrWhiteSpace(raw)
                ? null
            : DateOnly.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date
            )
                ? date.AddDays(offsetDays)
            : null;
    }
}
