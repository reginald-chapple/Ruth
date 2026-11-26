using System.Text.RegularExpressions;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Event calendar aggregate governing public and internal event listings and iCal feeds.</summary>
public sealed partial class EventCalendar
    : AggregateRoot<EventCalendarId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Events.EventCalendar";

    private EventCalendar() { }

    private EventCalendar(EventCalendarId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconCssClass { get; private set; }
    public string? ColorHex { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public bool IsIndexEnabled { get; private set; } = true;
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

    public static Result<EventCalendar> Create(EventCalendarSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var calendar = new EventCalendar(EventCalendarId.Create());
        var updated = calendar.Update(spec);
        return updated.IsFailure ? Result.Failure<EventCalendar>(updated.Errors) : calendar;
    }

    public Result Update(EventCalendarSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var normalizedSlug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (
            string.IsNullOrWhiteSpace(spec.Name)
            || spec.Name.Trim().Length > 160
            || !CalendarSlugRegex.IsMatch(normalizedSlug)
        )
        {
            return EventsErrors.CalendarInvalid;
        }

        Name = spec.Name.Trim();
        Slug = normalizedSlug;
        Description = TrimOptional(spec.Description, 1000);
        IconCssClass = TrimOptional(spec.IconCssClass, 120);
        ColorHex = TrimOptional(spec.ColorHex, 16);
        IsPublic = spec.IsPublic;
        IsIndexEnabled = spec.IsIndexEnabled;
        IsActive = spec.IsActive;
        return Result.Success();
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9-]{1,95}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex CalendarSlugRegex { get; }
}
