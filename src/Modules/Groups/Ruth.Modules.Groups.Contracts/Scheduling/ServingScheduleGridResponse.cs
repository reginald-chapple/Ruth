namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Full Coordinator Schedule Grid matrix for a Group over a date range.</summary>
public sealed record ServingScheduleGridResponse(
    Guid GroupId,
    string GroupName,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<ServingScheduleSlotResponse> Slots,
    IReadOnlyList<ScheduleCandidateResponse> Candidates
);
