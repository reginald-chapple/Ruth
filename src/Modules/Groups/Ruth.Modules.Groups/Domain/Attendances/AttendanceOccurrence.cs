using Ruth.Modules.Groups.Contracts.Events;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Attendances;

/// <summary>Aggregate representing a single dated occurrence of a Group + Location + Schedule and its attendances/RSVPs.</summary>
public sealed class AttendanceOccurrence
    : AggregateRoot<AttendanceOccurrenceId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasIntegrationEvents
{
    /// <summary>Canonical entity type key for AttendanceOccurrence.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.AttendanceOccurrence";

    private readonly List<Attendance> _attendances = [];
    private readonly List<IIntegrationEvent> _integrationEvents = [];

    private AttendanceOccurrence() { }

    private AttendanceOccurrence(
        AttendanceOccurrenceId id,
        GroupId? groupId,
        GroupTypeId? rootGroupTypeId,
        Guid? locationId,
        Guid? scheduleId,
        DateOnly occurrenceDate
    )
        : base(id)
    {
        GroupId = groupId;
        RootGroupTypeId = rootGroupTypeId;
        LocationId = locationId;
        ScheduleId = scheduleId;
        OccurrenceDate = occurrenceDate;
        SundayDate = SundayDateCalculator.ComputeSundayDate(occurrenceDate);
        OccurrenceDateKey = SundayDateCalculator.ComputeDateKey(occurrenceDate);
    }

    /// <summary>Gets the optional Group identifier.</summary>
    public GroupId? GroupId { get; private set; }

    /// <summary>Gets the optional root GroupType identifier for reporting.</summary>
    public GroupTypeId? RootGroupTypeId { get; private set; }

    /// <summary>Gets the optional Core Location identifier.</summary>
    public Guid? LocationId { get; private set; }

    /// <summary>Gets the optional Core Schedule identifier.</summary>
    public Guid? ScheduleId { get; private set; }

    /// <summary>Gets the calendar date of this occurrence.</summary>
    public DateOnly OccurrenceDate { get; private set; }

    /// <summary>Gets the computed week-ending Sunday date.</summary>
    public DateOnly SundayDate { get; private set; }

    /// <summary>Gets the computed yyyyMMdd integer date key.</summary>
    public int OccurrenceDateKey { get; private set; }

    /// <summary>Gets optional occurrence display name.</summary>
    public string? Name { get; private set; }

    /// <summary>Gets whether the occurrence was cancelled / did not meet.</summary>
    public bool? DidNotOccur { get; private set; }

    /// <summary>Gets optional leader notes for the occurrence.</summary>
    public string? Notes { get; private set; }

    /// <summary>Gets optional anonymous headcount.</summary>
    public int? AnonymousAttendanceCount { get; private set; }

    /// <summary>Gets optional RSVP accept confirmation message.</summary>
    public string? AcceptConfirmationMessage { get; private set; }

    /// <summary>Gets optional RSVP decline confirmation message.</summary>
    public string? DeclineConfirmationMessage { get; private set; }

    /// <summary>Gets whether decline reasons should be displayed on RSVP links.</summary>
    public bool ShowDeclineReasons { get; private set; }

    /// <summary>Gets optional Core DefinedValue attendance type identifier.</summary>
    public Guid? AttendanceTypeValueId { get; private set; }

    /// <summary>Gets the attendance and RSVP lines for this occurrence.</summary>
    public IReadOnlyCollection<Attendance> Attendances => _attendances.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IIntegrationEvent> IntegrationEvents =>
        _integrationEvents.AsReadOnly();

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <inheritdoc />
    public void ClearIntegrationEvents()
    {
        _integrationEvents.Clear();
    }

    internal static AttendanceOccurrence Create(
        GroupId? groupId,
        GroupTypeId? rootGroupTypeId,
        Guid? locationId,
        Guid? scheduleId,
        DateOnly occurrenceDate,
        string? name = null
    )
    {
        return new AttendanceOccurrence(
            AttendanceOccurrenceId.Create(),
            groupId,
            rootGroupTypeId,
            locationId,
            scheduleId,
            occurrenceDate
        )
        {
            Name = name?.Trim(),
        };
    }

    internal Result UpdateOccurrenceMetadata(
        bool didNotOccur,
        int? anonymousAttendanceCount,
        string? notes,
        string? name
    )
    {
        if (anonymousAttendanceCount is < 0)
        {
            return GroupsErrors.InvalidAnonymousCount;
        }

        var notesResult = GroupsTextPolicy.NormalizeOptional(notes, GroupsTextPolicy.NoteMaxLength);
        var nameResult = GroupsTextPolicy.NormalizeOptional(
            name,
            GroupsTextPolicy.ShortLabelMaxLength,
            allowNewLines: false
        );
        var combined = Result.Combine(notesResult, nameResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        DidNotOccur = didNotOccur;
        Notes = notesResult.Value;
        Name = nameResult.Value ?? Name;

        if (didNotOccur)
        {
            AnonymousAttendanceCount = null;
            foreach (var attendance in _attendances)
            {
                attendance.ClearDidAttend();
            }
        }
        else
        {
            AnonymousAttendanceCount = anonymousAttendanceCount;
        }

        return Result.Success();
    }

    internal Result<Attendance> RecordPersonAttendance(
        Guid personId,
        Guid? campusId,
        bool didAttend,
        bool isFirstTime,
        string? note,
        Guid? checkedInByPersonId,
        bool countsAsWeekendService,
        DateTimeOffset nowUtc
    )
    {
        var noteResult = GroupsTextPolicy.NormalizeOptional(note, GroupsTextPolicy.NoteMaxLength);
        if (noteResult.IsFailure)
        {
            return Result.Failure<Attendance>(noteResult.Errors);
        }

        var effectiveDidAttend = DidNotOccur != true && didAttend;
        var existing = _attendances.FirstOrDefault(a => a.PersonId == personId);
        bool shouldEmitEvent;
        Attendance record;

        if (existing is not null)
        {
            shouldEmitEvent = existing.UpdateRosterAttendance(
                campusId,
                effectiveDidAttend,
                isFirstTime,
                noteResult.Value,
                checkedInByPersonId,
                nowUtc
            );
            record = existing;
        }
        else
        {
            record = Attendance.CreateForRoster(
                Id,
                personId,
                campusId,
                effectiveDidAttend,
                isFirstTime,
                noteResult.Value,
                checkedInByPersonId,
                nowUtc
            );
            _attendances.Add(record);
            shouldEmitEvent = effectiveDidAttend;
        }

        if (shouldEmitEvent)
        {
            StagePersonAttendedEvent(
                record,
                personId,
                campusId,
                isFirstTime,
                countsAsWeekendService,
                nowUtc
            );
        }

        return Result.Success(record);
    }

    private void StagePersonAttendedEvent(
        Attendance record,
        Guid personId,
        Guid? campusId,
        bool isFirstTime,
        bool countsAsWeekendService,
        DateTimeOffset nowUtc
    )
    {
        _integrationEvents.Add(
            new PersonAttendedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc.ToUniversalTime(),
                record.Id.Value,
                Id.Value,
                personId,
                GroupId?.Value,
                RootGroupTypeId?.Value,
                campusId,
                LocationId,
                ScheduleId,
                OccurrenceDate,
                SundayDate,
                isFirstTime,
                countsAsWeekendService
            )
        );
    }

    internal Attendance ScheduleVolunteer(
        Guid personId,
        Guid? campusId,
        Guid? scheduledByPersonId,
        bool autoConfirm,
        DateTimeOffset nowUtc
    )
    {
        var existing = _attendances.FirstOrDefault(a => a.PersonId == personId);
        if (existing is not null)
        {
            existing.Reschedule(scheduledByPersonId, autoConfirm, nowUtc);
            return existing;
        }

        var created = Attendance.CreateForSchedule(
            Id,
            personId,
            campusId,
            scheduledByPersonId,
            autoConfirm,
            nowUtc
        );
        _attendances.Add(created);
        return created;
    }

    internal bool RemoveAttendance(AttendanceId attendanceId)
    {
        var item = _attendances.FirstOrDefault(a => a.Id == attendanceId);
        return item is not null && _attendances.Remove(item);
    }
}
