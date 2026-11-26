namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Participant row displayed on the live Room/Facility Manager tablet roster.</summary>
/// <param name="SessionId">Owning check-in session identifier.</param>
/// <param name="EntryId">Attendance entry identifier.</param>
/// <param name="PersonId">Participant person identifier.</param>
/// <param name="PersonFullName">Participant display name.</param>
/// <param name="ScheduleId">Schedule identifier.</param>
/// <param name="Status">Current entry status.</param>
/// <param name="SecurityPickupCode">Issued daily pickup security code.</param>
/// <param name="AttendanceNote">Medical, allergy, or accommodation note.</param>
/// <param name="IsCapacityOverride">Whether capacity was overridden.</param>
/// <param name="IsEligibilityOverride">Whether eligibility was overridden.</param>
/// <param name="CheckedInOnUtc">UTC check-in timestamp.</param>
/// <param name="CheckedOutOnUtc">Optional UTC check-out timestamp.</param>
public sealed record LiveRoomParticipantDto(
    Guid SessionId,
    Guid EntryId,
    Guid PersonId,
    string PersonFullName,
    Guid ScheduleId,
    CheckInEntryStatus Status,
    string? SecurityPickupCode,
    string? AttendanceNote,
    bool IsCapacityOverride,
    bool IsEligibilityOverride,
    DateTimeOffset CheckedInOnUtc,
    DateTimeOffset? CheckedOutOnUtc
);
