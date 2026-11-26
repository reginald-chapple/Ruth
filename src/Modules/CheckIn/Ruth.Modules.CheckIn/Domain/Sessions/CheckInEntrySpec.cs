namespace Ruth.Modules.CheckIn.Domain.Sessions;

internal sealed record CheckInEntrySpec(
    CheckInAreaId AreaId,
    string AreaName,
    Guid? LinkedGroupId,
    Guid PersonId,
    string PersonFullName,
    Guid LocationId,
    string LocationName,
    Guid ScheduleId,
    DateOnly ServiceDate,
    string? SecurityPickupCode,
    bool PrintBadgeRequested,
    bool IsCapacityOverride,
    bool IsEligibilityOverride,
    Guid? OverrideByPersonId,
    string? OverrideReason,
    string? AttendanceNote,
    DateTimeOffset CheckedInOnUtc
);
