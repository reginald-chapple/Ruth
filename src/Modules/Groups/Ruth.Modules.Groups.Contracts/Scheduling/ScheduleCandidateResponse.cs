namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Eligible volunteer candidate displayed in the Coordinator Schedule Grid drawer.</summary>
public sealed record ScheduleCandidateResponse(
    Guid GroupMemberId,
    Guid PersonId,
    string GroupRoleName,
    bool HasBlackoutOnDate,
    bool HasRequirementGap,
    bool HasDoubleBookingOnDate,
    bool MatchesTemplateOnDate,
    bool IsEligible
);
