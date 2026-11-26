using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Sessions;

/// <summary>Individual participant check-in attendance entry belonging to an <see cref="AttendanceCheckInSession" />.</summary>
public sealed class CheckInAttendanceEntry : Entity<CheckInAttendanceEntryId>
{
    private CheckInAttendanceEntry() { }

    private CheckInAttendanceEntry(
        CheckInAttendanceEntryId id,
        AttendanceCheckInSessionId sessionId,
        CheckInConfigurationId configurationId
    )
        : base(id)
    {
        SessionId = sessionId;
        CheckInConfigurationId = configurationId;
    }

    /// <summary>Gets the parent check-in session identifier.</summary>
    public AttendanceCheckInSessionId SessionId { get; private set; }

    /// <summary>Gets the check-in configuration identifier.</summary>
    public CheckInConfigurationId CheckInConfigurationId { get; private set; }

    /// <summary>Gets the check-in area identifier.</summary>
    public CheckInAreaId CheckInAreaId { get; private set; }

    /// <summary>Gets the snapshot check-in area name.</summary>
    public string AreaName { get; private set; } = string.Empty;

    /// <summary>Gets the optional linked Group identifier.</summary>
    public Guid? LinkedGroupId { get; private set; }

    /// <summary>Gets the participant person identifier.</summary>
    public Guid PersonId { get; private set; }

    /// <summary>Gets the participant full display name snapshot.</summary>
    public string PersonFullName { get; private set; } = string.Empty;

    /// <summary>Gets the assigned room/location identifier.</summary>
    public Guid LocationId { get; private set; }

    /// <summary>Gets the assigned room/location display label.</summary>
    public string LocationName { get; private set; } = string.Empty;

    /// <summary>Gets the schedule identifier.</summary>
    public Guid ScheduleId { get; private set; }

    /// <summary>Gets the local service date.</summary>
    public DateOnly ServiceDate { get; private set; }

    /// <summary>Gets the current attendance lifecycle status.</summary>
    public CheckInEntryStatus Status { get; private set; }

    /// <summary>Gets the issued daily security pickup code when enabled on the area.</summary>
    public string? SecurityPickupCode { get; private set; }

    /// <summary>Gets whether badge label printing was requested for this entry.</summary>
    public bool PrintBadgeRequested { get; private set; }

    /// <summary>Gets whether room capacity/closed status was overridden by staff.</summary>
    public bool IsCapacityOverride { get; private set; }

    /// <summary>Gets whether participant eligibility was overridden by staff.</summary>
    public bool IsEligibilityOverride { get; private set; }

    /// <summary>Gets the optional staff person who authorized an override or room move.</summary>
    public Guid? OverrideByPersonId { get; private set; }

    /// <summary>Gets the audit justification for an override or room move.</summary>
    public string? OverrideReason { get; private set; }

    /// <summary>Gets optional medical, allergy, or accommodation notes captured at check-in.</summary>
    public string? AttendanceNote { get; private set; }

    /// <summary>Gets the UTC timestamp when the participant checked in.</summary>
    public DateTimeOffset CheckedInOnUtc { get; private set; }

    /// <summary>Gets the optional UTC timestamp when the participant checked out.</summary>
    public DateTimeOffset? CheckedOutOnUtc { get; private set; }

    /// <summary>Gets the optional guardian or staff person who completed check-out.</summary>
    public Guid? CheckedOutByPersonId { get; private set; }

    /// <summary>Gets the verified security pickup code presented at check-out.</summary>
    public string? VerifiedPickupCode { get; private set; }

    internal static Result<CheckInAttendanceEntry> Create(
        AttendanceCheckInSessionId sessionId,
        CheckInConfigurationId configurationId,
        CheckInEntrySpec spec
    )
    {
        var guardResult = ValidateSpec(spec);
        if (guardResult.IsFailure)
        {
            return Result.Failure<CheckInAttendanceEntry>(guardResult.Errors);
        }

        var noteResult = CheckInTextPolicy.NormalizeOptional(spec.AttendanceNote, maxLength: 500);
        var reasonResult = CheckInTextPolicy.NormalizeOptional(spec.OverrideReason, maxLength: 500);
        var combined = Result.Combine(noteResult, reasonResult);
        if (combined.IsFailure)
        {
            return Result.Failure<CheckInAttendanceEntry>(combined.Errors);
        }

        var entry = new CheckInAttendanceEntry(
            CheckInAttendanceEntryId.Create(),
            sessionId,
            configurationId
        )
        {
            CheckInAreaId = spec.AreaId,
            AreaName = spec.AreaName.Trim(),
            LinkedGroupId = spec.LinkedGroupId is { } gid && gid != Guid.Empty ? gid : null,
            PersonId = spec.PersonId,
            PersonFullName = string.IsNullOrWhiteSpace(spec.PersonFullName)
                ? "Participant"
                : spec.PersonFullName.Trim(),
            LocationId = spec.LocationId,
            LocationName = spec.LocationName.Trim(),
            ScheduleId = spec.ScheduleId,
            ServiceDate = spec.ServiceDate,
            Status = CheckInEntryStatus.CheckedIn,
            SecurityPickupCode = string.IsNullOrWhiteSpace(spec.SecurityPickupCode)
                ? null
                : spec.SecurityPickupCode.Trim().ToUpperInvariant(),
            PrintBadgeRequested = spec.PrintBadgeRequested,
            IsCapacityOverride = spec.IsCapacityOverride,
            IsEligibilityOverride = spec.IsEligibilityOverride,
            OverrideByPersonId = NormalizeOptionalGuid(spec.OverrideByPersonId),
            OverrideReason = reasonResult.Value,
            AttendanceNote = noteResult.Value,
            CheckedInOnUtc = spec.CheckedInOnUtc.ToUniversalTime(),
        };

        return Result.Success(entry);
    }

    internal Result CheckOut(
        DateTimeOffset checkedOutOnUtc,
        Guid? checkedOutByPersonId,
        string? providedPickupCode,
        bool isStaffOverride,
        string? overrideReason
    )
    {
        var guardResult = ValidateCheckOutGuards(
            providedPickupCode,
            isStaffOverride,
            overrideReason
        );
        if (guardResult.IsFailure)
        {
            return guardResult;
        }

        var reasonResult = CheckInTextPolicy.NormalizeOptional(overrideReason, maxLength: 500);
        if (reasonResult.IsFailure)
        {
            return reasonResult;
        }

        ApplyCheckOutState(
            checkedOutOnUtc,
            checkedOutByPersonId,
            providedPickupCode,
            isStaffOverride,
            reasonResult.Value
        );
        return Result.Success();
    }

    private Result ValidateCheckOutGuards(
        string? providedPickupCode,
        bool isStaffOverride,
        string? overrideReason
    )
    {
        return Status != CheckInEntryStatus.CheckedIn ? CheckInErrors.EntryNotActivelyCheckedIn
            : isStaffOverride
                ? (
                    string.IsNullOrWhiteSpace(overrideReason)
                        ? CheckInErrors.OverrideRequiresStaffAndReason
                        : Result.Success()
                )
            : MatchesIssuedPickupCode(providedPickupCode) ? Result.Success()
            : CheckInErrors.InvalidSecurityPickupCode;
    }

    private bool MatchesIssuedPickupCode(string? providedPickupCode)
    {
        if (string.IsNullOrWhiteSpace(SecurityPickupCode))
        {
            return true;
        }

        var normalizedInput = providedPickupCode?.Trim().ToUpperInvariant();
        return string.Equals(SecurityPickupCode, normalizedInput, StringComparison.Ordinal);
    }

    private void ApplyCheckOutState(
        DateTimeOffset checkedOutOnUtc,
        Guid? checkedOutByPersonId,
        string? providedPickupCode,
        bool isStaffOverride,
        string? normalizedReason
    )
    {
        Status = CheckInEntryStatus.CheckedOut;
        CheckedOutOnUtc = checkedOutOnUtc.ToUniversalTime();
        CheckedOutByPersonId = NormalizeOptionalGuid(checkedOutByPersonId);
        VerifiedPickupCode = string.IsNullOrWhiteSpace(providedPickupCode)
            ? null
            : providedPickupCode.Trim().ToUpperInvariant();
        if (isStaffOverride)
        {
            OverrideByPersonId = CheckedOutByPersonId;
            OverrideReason = normalizedReason;
        }
    }

    internal Result ReassignRoom(
        CheckInAreaId targetAreaId,
        string targetAreaName,
        Guid targetLocationId,
        string targetLocationName,
        Guid? overrideByPersonId,
        string? reason,
        bool isCapacityOverride
    )
    {
        var guardResult = ValidateReassignGuards(targetLocationId, reason, isCapacityOverride);
        if (guardResult.IsFailure)
        {
            return guardResult;
        }

        var reasonResult = CheckInTextPolicy.NormalizeOptional(reason, maxLength: 500);
        if (reasonResult.IsFailure)
        {
            return reasonResult;
        }

        CheckInAreaId = targetAreaId;
        AreaName = targetAreaName.Trim();
        LocationId = targetLocationId;
        LocationName = targetLocationName.Trim();
        IsCapacityOverride = IsCapacityOverride || isCapacityOverride;
        OverrideByPersonId = NormalizeOptionalGuid(overrideByPersonId);
        OverrideReason = reasonResult.Value ?? OverrideReason;
        return Result.Success();
    }

    private Result ValidateReassignGuards(
        Guid targetLocationId,
        string? reason,
        bool isCapacityOverride
    )
    {
        return Status != CheckInEntryStatus.CheckedIn ? CheckInErrors.EntryNotActivelyCheckedIn
            : targetLocationId == Guid.Empty ? CheckInErrors.LocationRequired
            : isCapacityOverride && string.IsNullOrWhiteSpace(reason)
                ? CheckInErrors.OverrideRequiresStaffAndReason
            : Result.Success();
    }

    internal void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        PersonId = ReplaceIfMatching(PersonId, retiredPersonId, survivingPersonId);
        CheckedOutByPersonId = ReplaceOptionalIfMatching(
            CheckedOutByPersonId,
            retiredPersonId,
            survivingPersonId
        );
        OverrideByPersonId = ReplaceOptionalIfMatching(
            OverrideByPersonId,
            retiredPersonId,
            survivingPersonId
        );
    }

    private static Guid ReplaceIfMatching(Guid current, Guid retiredId, Guid survivingId)
    {
        return current == retiredId ? survivingId : current;
    }

    private static Guid? ReplaceOptionalIfMatching(Guid? current, Guid retiredId, Guid survivingId)
    {
        return current == retiredId ? survivingId : current;
    }

    private static Result ValidateSpec(CheckInEntrySpec spec)
    {
        return spec.PersonId == Guid.Empty ? CheckInErrors.PersonRequired
            : spec.LocationId == Guid.Empty ? CheckInErrors.LocationRequired
            : spec.ScheduleId == Guid.Empty ? CheckInErrors.ScheduleRequired
            : (spec.IsCapacityOverride || spec.IsEligibilityOverride)
            && string.IsNullOrWhiteSpace(spec.OverrideReason)
                ? CheckInErrors.OverrideRequiresStaffAndReason
            : Result.Success();
    }

    private static Guid? NormalizeOptionalGuid(Guid? raw)
    {
        return raw is { } id && id != Guid.Empty ? id : null;
    }
}
