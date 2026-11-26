using Ruth.Modules.CheckIn.Contracts.Configurations;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Individual participant selection inside a <see cref="SubmitCheckInSessionCommand" />.</summary>
/// <param name="PersonId">Participant person identifier.</param>
/// <param name="AreaId">Selected check-in area identifier.</param>
/// <param name="LocationId">Optional explicit room/location identifier (auto-balanced when null).</param>
/// <param name="ScheduleId">Optional item-specific schedule override.</param>
/// <param name="PersonFullName">Optional fallback display name when CRM profile is supplied externally.</param>
/// <param name="ParticipantContext">Optional explicit participant context for eligibility evaluation.</param>
/// <param name="AttendanceNote">Optional session note (accommodations, allergies, pickup instructions).</param>
/// <param name="IsCapacityOverride">Whether a staff member explicitly overrode closed/firm-capacity limits.</param>
/// <param name="IsEligibilityOverride">Whether a staff member explicitly overrode area eligibility constraints.</param>
/// <param name="OverrideReason">Required audit justification when an override flag is set.</param>
public sealed record CheckInSelectionItemDto(
    Guid PersonId,
    Guid AreaId,
    Guid? LocationId = null,
    Guid? ScheduleId = null,
    string? PersonFullName = null,
    CheckInParticipantContextDto? ParticipantContext = null,
    string? AttendanceNote = null,
    bool IsCapacityOverride = false,
    bool IsEligibilityOverride = false,
    string? OverrideReason = null
);
