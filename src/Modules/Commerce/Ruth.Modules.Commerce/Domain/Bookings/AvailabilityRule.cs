using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Availability or blackout rule governing when a BookableService, provider, or RentableResource is open.</summary>
public sealed class AvailabilityRule
    : AggregateRoot<AvailabilityRuleId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Commerce.AvailabilityRule";

    private AvailabilityRule() { }

    private AvailabilityRule(AvailabilityRuleId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public BookingTargetKind TargetKind { get; private set; }
    public AvailabilityRuleType RuleType { get; private set; }
    public BookableServiceId? BookableServiceId { get; private set; }
    public RentableResourceId? RentableResourceId { get; private set; }
    public Guid? ProviderPersonId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid? ScheduleId { get; private set; }
    public DayOfWeek? DayOfWeek { get; private set; }
    public TimeOnly? StartLocalTime { get; private set; }
    public TimeOnly? EndLocalTime { get; private set; }
    public string TimeZoneId { get; private set; } = "UTC";
    public DateTimeOffset? CustomStartUtc { get; private set; }
    public DateTimeOffset? CustomEndUtc { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<AvailabilityRule> Create(AvailabilityRuleSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        return !IsValidSpec(spec, out var resolvedTimeZoneId)
            ? (Result<AvailabilityRule>)CommerceErrors.AvailabilityRuleInvalid
            : (Result<AvailabilityRule>)
                new AvailabilityRule(AvailabilityRuleId.Create())
                {
                    Name = spec.Name.Trim(),
                    TargetKind = spec.TargetKind,
                    RuleType = spec.RuleType,
                    BookableServiceId =
                        spec.TargetKind == BookingTargetKind.Service
                        && spec.BookableServiceId is { } sId
                            ? Domain.BookableServiceId.From(sId)
                            : null,
                    RentableResourceId =
                        spec.TargetKind == BookingTargetKind.Resource
                        && spec.RentableResourceId is { } rId
                            ? Domain.RentableResourceId.From(rId)
                            : null,
                    ProviderPersonId = NormalizeGuid(spec.ProviderPersonId),
                    LocationId = NormalizeGuid(spec.LocationId),
                    ScheduleId = NormalizeGuid(spec.ScheduleId),
                    DayOfWeek = spec.DayOfWeek,
                    StartLocalTime = spec.StartLocalTime,
                    EndLocalTime = spec.EndLocalTime,
                    TimeZoneId = resolvedTimeZoneId,
                    CustomStartUtc = spec.CustomStartUtc?.ToUniversalTime(),
                    CustomEndUtc = spec.CustomEndUtc?.ToUniversalTime(),
                    IsActive = spec.IsActive,
                };
    }

    public IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)> ExpandDirectWindows(
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc
    )
    {
        return !IsActive || rangeEndUtc <= rangeStartUtc ? []
            : IsDirectUtcRule() ? ExpandCustomOrBlackoutWindow(rangeStartUtc, rangeEndUtc)
            : IsWeeklyRecurringRule() ? ExpandWeeklyWindows(rangeStartUtc, rangeEndUtc)
            : [];
    }

    private bool IsDirectUtcRule()
    {
        return RuleType
                is AvailabilityRuleType.CustomDateWindow
                    or AvailabilityRuleType.BlackoutWindow
            && CustomStartUtc.HasValue
            && CustomEndUtc.HasValue;
    }

    private IReadOnlyList<(
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc
    )> ExpandCustomOrBlackoutWindow(DateTimeOffset rangeStartUtc, DateTimeOffset rangeEndUtc)
    {
        var s = CustomStartUtc!.Value;
        var e = CustomEndUtc!.Value;
        return s < rangeEndUtc && e > rangeStartUtc ? [(s, e)] : [];
    }

    private bool IsWeeklyRecurringRule()
    {
        return RuleType == AvailabilityRuleType.RecurringWindow
            && DayOfWeek.HasValue
            && StartLocalTime.HasValue
            && EndLocalTime.HasValue;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ProviderPersonId != retiredPersonId)
        {
            return false;
        }

        ProviderPersonId = survivingPersonId;
        return true;
    }

    private List<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)> ExpandWeeklyWindows(
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc
    )
    {
        var tz = TimeZoneInfo.TryFindSystemTimeZoneById(TimeZoneId, out var found)
            ? found
            : TimeZoneInfo.Utc;
        var localStartDate = DateOnly
            .FromDateTime(TimeZoneInfo.ConvertTime(rangeStartUtc, tz).DateTime)
            .AddDays(-1);
        var localEndDate = DateOnly
            .FromDateTime(TimeZoneInfo.ConvertTime(rangeEndUtc, tz).DateTime)
            .AddDays(1);

        var windows = new List<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>();
        for (var d = localStartDate; d <= localEndDate; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek!.Value)
            {
                continue;
            }

            var startUtc = ToUtcSafe(d, StartLocalTime!.Value, tz);
            var endUtc = ToUtcSafe(d, EndLocalTime!.Value, tz);
            if (endUtc > startUtc && startUtc < rangeEndUtc && endUtc > rangeStartUtc)
            {
                windows.Add((startUtc, endUtc));
            }
        }

        return windows;
    }

    private static DateTimeOffset ToUtcSafe(DateOnly date, TimeOnly time, TimeZoneInfo tz)
    {
        var unspecified = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        var offset = tz.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset).ToUniversalTime();
    }

    private static bool IsValidSpec(AvailabilityRuleSpec spec, out string resolvedTimeZoneId)
    {
        resolvedTimeZoneId = string.IsNullOrWhiteSpace(spec.TimeZoneId)
            ? "UTC"
            : spec.TimeZoneId.Trim();

        return HasValidNameAndTimeZone(spec.Name, resolvedTimeZoneId)
            && HasValidTarget(spec)
            && HasValidWindowPayload(spec);
    }

    private static bool HasValidNameAndTimeZone(string? name, string resolvedTimeZoneId)
    {
        return !string.IsNullOrWhiteSpace(name)
            && name.Trim().Length <= 160
            && TimeZoneInfo.TryFindSystemTimeZoneById(resolvedTimeZoneId, out _);
    }

    private static bool HasValidTarget(AvailabilityRuleSpec spec)
    {
        return spec.TargetKind switch
        {
            BookingTargetKind.Service => NormalizeGuid(spec.BookableServiceId).HasValue,
            BookingTargetKind.Resource => NormalizeGuid(spec.RentableResourceId).HasValue,
            _ => false,
        };
    }

    private static bool HasValidWindowPayload(AvailabilityRuleSpec spec)
    {
        var hasSchedule = NormalizeGuid(spec.ScheduleId).HasValue;
        var hasWeekly = HasValidWeeklyTimes(spec);
        var hasCustom = HasValidCustomRange(spec);

        return spec.RuleType switch
        {
            AvailabilityRuleType.RecurringWindow => hasSchedule || hasWeekly,
            AvailabilityRuleType.CustomDateWindow => hasCustom,
            AvailabilityRuleType.BlackoutWindow => hasCustom || hasSchedule,
            _ => false,
        };
    }

    private static bool HasValidWeeklyTimes(AvailabilityRuleSpec spec)
    {
        return spec.DayOfWeek.HasValue
            && spec.StartLocalTime.HasValue
            && spec.EndLocalTime.HasValue
            && spec.EndLocalTime.Value > spec.StartLocalTime.Value;
    }

    private static bool HasValidCustomRange(AvailabilityRuleSpec spec)
    {
        return spec.CustomStartUtc.HasValue
            && spec.CustomEndUtc.HasValue
            && spec.CustomEndUtc.Value > spec.CustomStartUtc.Value;
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }
}
