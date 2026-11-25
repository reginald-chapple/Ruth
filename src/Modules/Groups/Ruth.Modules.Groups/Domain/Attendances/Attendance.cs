using System.Security.Cryptography;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Attendances;

/// <summary>Individual person attendance and/or volunteer serving RSVP record within an AttendanceOccurrence.</summary>
public sealed class Attendance : Entity<AttendanceId>, IAuditableEntity
{
    private Attendance() { }

    private Attendance(
        AttendanceId id,
        AttendanceOccurrenceId occurrenceId,
        Guid? personId,
        DateTimeOffset startDateTimeUtc
    )
        : base(id)
    {
        OccurrenceId = occurrenceId;
        PersonId = personId;
        StartDateTimeUtc = startDateTimeUtc.ToUniversalTime();
    }

    /// <summary>Gets the owning AttendanceOccurrence identifier.</summary>
    public AttendanceOccurrenceId OccurrenceId { get; private set; }

    /// <summary>Gets the optional CRM Person identifier.</summary>
    public Guid? PersonId { get; private set; }

    /// <summary>Gets the optional Core Branch identifier.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the optional Core Device identifier.</summary>
    public Guid? DeviceId { get; private set; }

    /// <summary>Gets the attendance start or check-in UTC timestamp.</summary>
    public DateTimeOffset StartDateTimeUtc { get; private set; }

    /// <summary>Gets the optional attendance end or check-out UTC timestamp.</summary>
    public DateTimeOffset? EndDateTimeUtc { get; private set; }

    /// <summary>Gets whether the person attended.</summary>
    public bool? DidAttend { get; private set; }

    /// <summary>Gets whether this is the person's first recorded attendance.</summary>
    public bool? IsFirstTime { get; private set; }

    /// <summary>Gets optional attendance note.</summary>
    public string? Note { get; private set; }

    /// <summary>Gets the RSVP status.</summary>
    public RsvpStatus Rsvp { get; private set; } = RsvpStatus.Unknown;

    /// <summary>Gets when the volunteer responded to the RSVP.</summary>
    public DateTimeOffset? RsvpDateTimeUtc { get; private set; }

    /// <summary>Gets the cryptographically random URL-safe RSVP token for one-click Accept/Decline links.</summary>
    public string? RsvpToken { get; private set; }

    /// <summary>Gets the optional Core DefinedValue decline reason identifier.</summary>
    public Guid? DeclineReasonValueId { get; private set; }

    /// <summary>Gets optional free-text decline reason note.</summary>
    public string? DeclineReasonNote { get; private set; }

    /// <summary>Gets whether the volunteer was requested to attend.</summary>
    public bool? RequestedToAttend { get; private set; }

    /// <summary>Gets whether the volunteer is confirmed/scheduled to attend.</summary>
    public bool? ScheduledToAttend { get; private set; }

    /// <summary>Gets the coordinator PersonId who scheduled this volunteer.</summary>
    public Guid? ScheduledByPersonId { get; private set; }

    /// <summary>Gets whether a schedule confirmation notification was sent.</summary>
    public bool? ScheduleConfirmationSent { get; private set; }

    /// <summary>Gets whether a schedule reminder notification was sent.</summary>
    public bool? ScheduleReminderSent { get; private set; }

    /// <summary>Gets the PersonId who checked in or recorded this attendance.</summary>
    public Guid? CheckedInByPersonId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    internal static Attendance CreateForRoster(
        AttendanceOccurrenceId occurrenceId,
        Guid personId,
        Guid? branchId,
        bool didAttend,
        bool isFirstTime,
        string? note,
        Guid? checkedInByPersonId,
        DateTimeOffset nowUtc
    )
    {
        return new Attendance(AttendanceId.Create(), occurrenceId, personId, nowUtc)
        {
            BranchId = branchId,
            DidAttend = didAttend,
            IsFirstTime = didAttend ? isFirstTime : null,
            Note = note,
            CheckedInByPersonId = checkedInByPersonId,
        };
    }

    internal static Attendance CreateForSchedule(
        AttendanceOccurrenceId occurrenceId,
        Guid personId,
        Guid? branchId,
        Guid? scheduledByPersonId,
        bool autoConfirm,
        DateTimeOffset nowUtc
    )
    {
        var utc = nowUtc.ToUniversalTime();
        return new Attendance(AttendanceId.Create(), occurrenceId, personId, utc)
        {
            BranchId = branchId,
            RequestedToAttend = true,
            ScheduledToAttend = autoConfirm,
            Rsvp = autoConfirm ? RsvpStatus.Yes : RsvpStatus.Unknown,
            RsvpDateTimeUtc = autoConfirm ? utc : null,
            RsvpToken = GenerateRsvpToken(),
            ScheduledByPersonId = scheduledByPersonId,
            ScheduleConfirmationSent = true,
            DidAttend = null,
        };
    }

    internal bool UpdateRosterAttendance(
        Guid? branchId,
        bool didAttend,
        bool isFirstTime,
        string? note,
        Guid? checkedInByPersonId,
        DateTimeOffset nowUtc
    )
    {
        var transitionedToAttended = DidAttend != true && didAttend;
        BranchId ??= branchId;
        DidAttend = didAttend;
        if (transitionedToAttended)
        {
            IsFirstTime = isFirstTime;
            StartDateTimeUtc = nowUtc.ToUniversalTime();
        }
        else if (!didAttend)
        {
            IsFirstTime = null;
        }

        Note = note;
        CheckedInByPersonId = checkedInByPersonId ?? CheckedInByPersonId;
        return transitionedToAttended;
    }

    internal void ClearDidAttend()
    {
        DidAttend = false;
        IsFirstTime = null;
    }

    internal Result ApplyRsvp(
        RsvpStatus response,
        Guid? declineReasonValueId,
        string? declineReasonNote,
        DateTimeOffset nowUtc
    )
    {
        var noteResult = GroupsTextPolicy.NormalizeOptional(
            declineReasonNote,
            GroupsTextPolicy.NoteMaxLength
        );
        if (noteResult.IsFailure)
        {
            return noteResult;
        }

        Rsvp = response;
        RsvpDateTimeUtc = nowUtc.ToUniversalTime();
        RequestedToAttend = true;
        ScheduledToAttend = response == RsvpStatus.Yes;
        DeclineReasonValueId = response == RsvpStatus.No ? declineReasonValueId : null;
        DeclineReasonNote = response == RsvpStatus.No ? noteResult.Value : null;
        return Result.Success();
    }

    internal void Reschedule(Guid? scheduledByPersonId, bool autoConfirm, DateTimeOffset nowUtc)
    {
        var utc = nowUtc.ToUniversalTime();
        RequestedToAttend = true;
        ScheduledToAttend = autoConfirm;
        Rsvp = autoConfirm ? RsvpStatus.Yes : RsvpStatus.Unknown;
        RsvpDateTimeUtc = autoConfirm ? utc : null;
        RsvpToken ??= GenerateRsvpToken();
        DeclineReasonValueId = null;
        DeclineReasonNote = null;
        ScheduledByPersonId = scheduledByPersonId ?? ScheduledByPersonId;
    }

    internal void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }

        if (ScheduledByPersonId == retiredPersonId)
        {
            ScheduledByPersonId = survivingPersonId;
        }

        if (CheckedInByPersonId == retiredPersonId)
        {
            CheckedInByPersonId = survivingPersonId;
        }
    }

    private static string GenerateRsvpToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(18);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
