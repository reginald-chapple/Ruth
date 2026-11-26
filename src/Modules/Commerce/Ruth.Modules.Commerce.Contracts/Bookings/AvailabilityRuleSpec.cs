namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Input specification for creating an availability window or blackout rule on a service, provider, or rentable resource.</summary>
/// <param name="Name">Descriptive rule label (for example Weekday Coaching Hours, Holiday Facility Closure).</param>
/// <param name="TargetKind">Whether this rule applies to a BookableService or RentableResource.</param>
/// <param name="RuleType">Recurring window, custom UTC window, or blackout window.</param>
/// <param name="BookableServiceId">Required when <paramref name="TargetKind" /> is <see cref="BookingTargetKind.Service" />.</param>
/// <param name="RentableResourceId">Required when <paramref name="TargetKind" /> is <see cref="BookingTargetKind.Resource" />.</param>
/// <param name="ProviderPersonId">Optional specific provider CRM person identifier for service rules.</param>
/// <param name="LocationId">Optional physical Core Location override.</param>
/// <param name="ScheduleId">Optional Core module iCalendar Schedule identifier for <see cref="AvailabilityRuleType.RecurringWindow" />.</param>
/// <param name="DayOfWeek">Optional recurring day of week when not backed by a Core Schedule.</param>
/// <param name="StartLocalTime">Optional local start time for day-of-week recurring rules.</param>
/// <param name="EndLocalTime">Optional local end time for day-of-week recurring rules.</param>
/// <param name="TimeZoneId">IANA or Windows time zone identifier (defaults to UTC).</param>
/// <param name="CustomStartUtc">Explicit UTC start timestamp for CustomDateWindow or BlackoutWindow.</param>
/// <param name="CustomEndUtc">Explicit UTC end timestamp for CustomDateWindow or BlackoutWindow.</param>
/// <param name="IsActive">Whether the rule is active.</param>
public sealed record AvailabilityRuleSpec(
    string Name,
    BookingTargetKind TargetKind,
    AvailabilityRuleType RuleType,
    Guid? BookableServiceId = null,
    Guid? RentableResourceId = null,
    Guid? ProviderPersonId = null,
    Guid? LocationId = null,
    Guid? ScheduleId = null,
    DayOfWeek? DayOfWeek = null,
    TimeOnly? StartLocalTime = null,
    TimeOnly? EndLocalTime = null,
    string TimeZoneId = "UTC",
    DateTimeOffset? CustomStartUtc = null,
    DateTimeOffset? CustomEndUtc = null,
    bool IsActive = true
);
