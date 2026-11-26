namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Individual participant check-in entry receipt.</summary>
/// <param name="EntryId">Check-in attendance entry identifier.</param>
/// <param name="PersonId">Participant person identifier.</param>
/// <param name="PersonFullName">Participant full display name.</param>
/// <param name="AreaId">Check-in area identifier.</param>
/// <param name="AreaName">Check-in area name.</param>
/// <param name="LinkedGroupId">Optional linked Group identifier.</param>
/// <param name="LocationId">Assigned room/location identifier.</param>
/// <param name="LocationName">Assigned room/location display name.</param>
/// <param name="ScheduleId">Schedule identifier.</param>
/// <param name="Status">Current entry status.</param>
/// <param name="SecurityPickupCode">Pickup security code when issued for this entry.</param>
/// <param name="PrintBadgeRequested">Whether badge label printing applies to this entry.</param>
/// <param name="IsCapacityOverride">Whether room capacity/availability was overridden.</param>
/// <param name="IsEligibilityOverride">Whether participant eligibility was overridden.</param>
/// <param name="AttendanceNote">Optional note attached to this entry.</param>
/// <param name="CheckedInOnUtc">UTC check-in timestamp.</param>
/// <param name="CheckedOutOnUtc">Optional UTC check-out timestamp.</param>
public sealed record CheckedInEntryReceiptDto(
    Guid EntryId,
    Guid PersonId,
    string PersonFullName,
    Guid AreaId,
    string AreaName,
    Guid? LinkedGroupId,
    Guid LocationId,
    string LocationName,
    Guid ScheduleId,
    CheckInEntryStatus Status,
    string? SecurityPickupCode,
    bool PrintBadgeRequested,
    bool IsCapacityOverride,
    bool IsEligibilityOverride,
    string? AttendanceNote,
    DateTimeOffset CheckedInOnUtc,
    DateTimeOffset? CheckedOutOnUtc
);
