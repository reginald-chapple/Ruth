namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Read projection representing an availability or blackout rule.</summary>
public sealed record AvailabilityRuleResponse(
    Guid Id,
    string Name,
    BookingTargetKind TargetKind,
    AvailabilityRuleType RuleType,
    Guid? BookableServiceId,
    Guid? RentableResourceId,
    Guid? ProviderPersonId,
    Guid? LocationId,
    Guid? ScheduleId,
    DayOfWeek? DayOfWeek,
    TimeOnly? StartLocalTime,
    TimeOnly? EndLocalTime,
    string TimeZoneId,
    DateTimeOffset? CustomStartUtc,
    DateTimeOffset? CustomEndUtc,
    bool IsActive,
    DateTimeOffset CreatedOnUtc
);
