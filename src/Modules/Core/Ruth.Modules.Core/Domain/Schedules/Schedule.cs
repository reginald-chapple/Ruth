using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Reusable or inline schedule aggregate supporting RFC 5545 RRULEs, check-in offsets, and materialized horizon dates.</summary>
public sealed class Schedule
    : AggregateRoot<ScheduleId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for Schedule.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Schedule";

    private Schedule() { }

    private Schedule(ScheduleId id, Guid systemGuid)
        : base(id)
    {
        SystemGuid = systemGuid;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the optional schedule display name (null for inline/custom schedules).</summary>
    public string? Name { get; private set; }

    /// <summary>Gets the optional abbreviated display name.</summary>
    public string? AbbreviatedName { get; private set; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the RFC 5545 iCalendar VCALENDAR/VEVENT content.</summary>
    public string? ICalendarContent { get; private set; }

    /// <summary>Gets minutes before schedule start when check-in opens (null disables check-in).</summary>
    public int? CheckInStartOffsetMinutes { get; private set; }

    /// <summary>Gets minutes after schedule start when check-in closes (null closes at event end).</summary>
    public int? CheckInEndOffsetMinutes { get; private set; }

    /// <summary>Gets the computed effective first occurrence date.</summary>
    public DateOnly? EffectiveStartDate { get; private set; }

    /// <summary>Gets the computed effective last occurrence date, or null when unbounded.</summary>
    public DateOnly? EffectiveEndDate { get; private set; }

    /// <summary>Gets the optional simple weekly day of week.</summary>
    public DayOfWeek? WeeklyDayOfWeek { get; private set; }

    /// <summary>Gets the optional simple weekly time of day.</summary>
    public TimeOnly? WeeklyTimeOfDay { get; private set; }

    /// <summary>Gets the optional owning category identifier.</summary>
    public CategoryId? CategoryId { get; private set; }

    /// <summary>Gets whether the schedule automatically inactivates after its final occurrence.</summary>
    public bool AutoInactivateWhenComplete { get; private set; }

    /// <summary>Gets whether the schedule is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether the schedule is publicly visible.</summary>
    public bool IsPublic { get; private set; } = true;

    /// <summary>Gets the display sort order.</summary>
    public int Order { get; private set; }

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
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        CategoryId.HasValue
            ? new SecuredEntityRef(
                Category.EntityTypeKeyName,
                CategoryId.Value.Value,
                GlobalDefaultSecured.Instance
            )
            : GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Gets whether check-in is currently enabled on this schedule.</summary>
    public bool IsCheckInEnabled => IsActive && CheckInStartOffsetMinutes.HasValue;

    /// <summary>Classifies the schedule kind (Named, Custom, Weekly, or None).</summary>
    public ScheduleKind GetKind()
    {
        return !string.IsNullOrWhiteSpace(ICalendarContent)
            ? string.IsNullOrWhiteSpace(Name)
                ? ScheduleKind.Custom
                : ScheduleKind.Named
            : WeeklyDayOfWeek.HasValue
                ? ScheduleKind.Weekly
                : ScheduleKind.None;
    }

    /// <summary>Returns the parsed iCalendar event specification, or null if not using iCalendar.</summary>
    public ICalEventSpec? TryParseSpec()
    {
        if (string.IsNullOrWhiteSpace(ICalendarContent))
        {
            return null;
        }

        var parsed = ICalendarParser.Parse(ICalendarContent);
        return parsed.IsSuccess ? parsed.Value : null;
    }

    /// <summary>Returns the human-readable schedule summary text.</summary>
    public string ToFriendlyText()
    {
        return ScheduleFriendlyTextFormatter.Format(
            TryParseSpec(),
            WeeklyDayOfWeek,
            WeeklyTimeOfDay
        );
    }

    /// <summary>Gets the event duration in whole minutes.</summary>
    public int GetDurationMinutes()
    {
        var spec = TryParseSpec();
        return spec is null
            ? 0
            : (int)Math.Round(spec.Duration.TotalMinutes, MidpointRounding.ToEven);
    }

    /// <summary>Indicates whether the schedule is missing essential recurrence data.</summary>
    public bool HasScheduleWarning()
    {
        var spec = TryParseSpec();
        return spec is null
            ? !WeeklyDayOfWeek.HasValue
            : spec.RRule
                is { Frequency: RecurrenceFrequency.Monthly, ByDays.Count: 0, ByMonthDays.Count: 0 }
                && spec.DtStart == default;
    }

    /// <summary>Evaluates concrete occurrences over a UTC window applying optional timezone and category exclusions.</summary>
    public IReadOnlyList<ScheduleOccurrenceDto> GetOccurrences(
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        string? timeZoneId = null,
        IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>? categoryExclusions = null
    )
    {
        var spec = ResolveEffectiveSpec(rangeStartUtc);
        if (spec is null)
        {
            return [];
        }

        var tz = ICalendarTimeZoneHelper.ResolveTimeZone(timeZoneId, spec.TimeZoneId);
        return ICalendarRecurrenceEvaluator.Evaluate(
            spec,
            rangeStartUtc,
            rangeEndUtc,
            tz,
            categoryExclusions
        );
    }

    /// <summary>Determines whether check-in is active at a given UTC instant (accounting for start/end offsets and cross-midnight windows).</summary>
    public bool IsCheckInActiveAt(
        DateTimeOffset utcInstant,
        string? timeZoneId = null,
        IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>? categoryExclusions = null
    )
    {
        if (!IsCheckInEnabled)
        {
            return false;
        }

        var lookback = TimeSpan.FromDays(2);
        var occurrences = GetOccurrences(
            utcInstant - lookback,
            utcInstant + lookback,
            timeZoneId,
            categoryExclusions
        );
        foreach (var occ in occurrences)
        {
            var checkInStart = occ.StartOnUtc.AddMinutes(-CheckInStartOffsetMinutes!.Value);
            var checkInEnd = CheckInEndOffsetMinutes.HasValue
                ? occ.StartOnUtc.AddMinutes(CheckInEndOffsetMinutes.Value)
                : occ.EndOnUtc;
            if (
                checkInEnd >= checkInStart
                && utcInstant >= checkInStart
                && utcInstant <= checkInEnd
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether the scheduled event itself is active at the given UTC instant.</summary>
    public bool IsScheduleActiveAt(
        DateTimeOffset utcInstant,
        string? timeZoneId = null,
        IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>? categoryExclusions = null
    )
    {
        if (!IsActive)
        {
            return false;
        }

        var lookback = TimeSpan.FromDays(2);
        var occurrences = GetOccurrences(
            utcInstant - lookback,
            utcInstant + lookback,
            timeZoneId,
            categoryExclusions
        );
        return occurrences.Any(occ => utcInstant >= occ.StartOnUtc && utcInstant <= occ.EndOnUtc);
    }

    /// <summary>Determines whether check-out is active at the given UTC instant (from CheckInStart through Event End).</summary>
    public bool IsActiveForCheckOutAt(
        DateTimeOffset utcInstant,
        string? timeZoneId = null,
        IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>? categoryExclusions = null
    )
    {
        if (!IsActive)
        {
            return false;
        }

        var lookback = TimeSpan.FromDays(2);
        var occurrences = GetOccurrences(
            utcInstant - lookback,
            utcInstant + lookback,
            timeZoneId,
            categoryExclusions
        );
        foreach (var occ in occurrences)
        {
            var windowStart = CheckInStartOffsetMinutes.HasValue
                ? occ.StartOnUtc.AddMinutes(-CheckInStartOffsetMinutes.Value)
                : occ.StartOnUtc;
            if (utcInstant >= windowStart && utcInstant <= occ.EndOnUtc)
            {
                return true;
            }
        }

        return false;
    }

    internal static Result<Schedule> Create(ScheduleInput input, Guid? systemGuid = null)
    {
        var id = ScheduleId.Create();
        var schedule = new Schedule(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value
        );
        var apply = schedule.Apply(input);
        return apply.IsFailure ? Result.Failure<Schedule>(apply.Errors) : Result.Success(schedule);
    }

    internal Result Update(ScheduleInput input)
    {
        return Apply(input);
    }

    private Result Apply(ScheduleInput input)
    {
        var nameResult = CoreTextPolicy.ValidateOptionalText(
            input.Name,
            maxLength: 50,
            allowNewLines: false
        );
        var abbrResult = CoreTextPolicy.ValidateOptionalText(
            input.AbbreviatedName,
            maxLength: 50,
            allowNewLines: false
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            input.Description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, abbrResult, descResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        if (input.CheckInStartOffsetMinutes is < 0 || input.CheckInEndOffsetMinutes is < 0)
        {
            return CoreScheduleErrors.ScheduleOffsetsInvalid;
        }

        ICalEventSpec? parsedSpec = null;
        if (!string.IsNullOrWhiteSpace(input.ICalendarContent))
        {
            var parseResult = ICalendarParser.Parse(input.ICalendarContent);
            if (parseResult.IsFailure)
            {
                return Result.Failure(parseResult.Errors);
            }

            parsedSpec = parseResult.Value;
        }

        Name = nameResult.Value;
        AbbreviatedName = abbrResult.Value ?? nameResult.Value;
        ICalendarContent = string.IsNullOrWhiteSpace(input.ICalendarContent)
            ? null
            : input.ICalendarContent.Trim();
        CheckInStartOffsetMinutes = input.CheckInStartOffsetMinutes;
        CheckInEndOffsetMinutes = input.CheckInEndOffsetMinutes;
        WeeklyDayOfWeek = input.WeeklyDayOfWeek;
        WeeklyTimeOfDay = input.WeeklyTimeOfDay;
        CategoryId = input.CategoryId;
        AutoInactivateWhenComplete = input.AutoInactivateWhenComplete;
        IsActive = input.IsActive;
        IsPublic = input.IsPublic;
        Order = Math.Max(0, input.Order);
        Description = descResult.Value
            ?? (string.IsNullOrWhiteSpace(Name) ? ToFriendlyText() : null);

        ComputeEffectiveDates(parsedSpec);
        return Result.Success();
    }

    private void ComputeEffectiveDates(ICalEventSpec? spec)
    {
        if (spec is null)
        {
            EffectiveStartDate = null;
            EffectiveEndDate = null;
            return;
        }

        EffectiveStartDate = DateOnly.FromDateTime(spec.DtStart);
        if (spec.RRule is null)
        {
            var lastStart =
                spec.RDates.Count > 0 ? spec.RDates.Append(spec.DtStart).Max() : spec.DtStart;
            EffectiveEndDate = DateOnly.FromDateTime(lastStart.Add(spec.Duration));
            return;
        }

        if (spec.RRule.Until.HasValue)
        {
            EffectiveEndDate = DateOnly.FromDateTime(spec.RRule.Until.Value.Add(spec.Duration));
            return;
        }

        if (spec.RRule.Count is > 0 and <= 999)
        {
            var tz = ICalendarTimeZoneHelper.ResolveTimeZone(spec.TimeZoneId);
            var startUtc = ICalendarTimeZoneHelper.ToUtcOffset(spec.DtStart, tz, spec.DtStartIsUtc);
            var evaluated = ICalendarRecurrenceEvaluator.Evaluate(
                spec,
                startUtc.AddDays(-1),
                startUtc.AddYears(100),
                tz
            );
            EffectiveEndDate =
                evaluated.Count > 0
                    ? DateOnly.FromDateTime(evaluated[^1].LocalEnd)
                    : EffectiveStartDate;
            return;
        }

        EffectiveEndDate = null;
    }

    private ICalEventSpec? ResolveEffectiveSpec(DateTimeOffset anchorUtc)
    {
        var parsed = TryParseSpec();
        if (parsed is not null)
        {
            return parsed;
        }

        if (!WeeklyDayOfWeek.HasValue)
        {
            return null;
        }

        var baseDate = anchorUtc.UtcDateTime.Date.AddDays(-7);
        while (baseDate.DayOfWeek != WeeklyDayOfWeek.Value)
        {
            baseDate = baseDate.AddDays(1);
        }

        var time = WeeklyTimeOfDay?.ToTimeSpan() ?? TimeSpan.Zero;
        var rule = new ICalRecurrenceRule(
            RecurrenceFrequency.Weekly,
            Interval: 1,
            Count: null,
            Until: null,
            UntilIsUtc: false,
            ByDays: [new ByDayRule(WeeklyDayOfWeek.Value)],
            ByMonthDays: [],
            ByMonths: [],
            BySetPos: [],
            WeekStart: DayOfWeek.Sunday
        );
        return new ICalEventSpec(
            baseDate.Add(time),
            DtStartIsUtc: false,
            IsAllDay: false,
            Duration: TimeSpan.FromHours(1),
            TimeZoneId: null,
            RRule: rule,
            RDates: [],
            ExDates: []
        );
    }
}
