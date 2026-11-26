using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Moves an active checked-in participant to a different room/location from the Room/Facility Manager UI.</summary>
/// <param name="SessionId">Owning check-in session identifier.</param>
/// <param name="EntryId">Check-in entry identifier.</param>
/// <param name="TargetAreaId">Target check-in area identifier.</param>
/// <param name="TargetLocationId">Target room/location identifier.</param>
/// <param name="OverrideByPersonId">Optional staff person identifier performing the move.</param>
/// <param name="Reason">Required audit reason when overriding closed/firm-capacity constraints.</param>
/// <param name="AllowCapacityOverride">Whether closed or firm-capacity room constraints may be overridden.</param>
public sealed record OverrideCheckInRoomAssignmentCommand(
    Guid SessionId,
    Guid EntryId,
    Guid TargetAreaId,
    Guid TargetLocationId,
    Guid? OverrideByPersonId = null,
    string? Reason = null,
    bool AllowCapacityOverride = true
) : ICommand<CheckedInEntryReceiptDto>;
