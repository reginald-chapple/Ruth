using System.Text.RegularExpressions;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Event item aggregate spanning one or more calendars, target audiences, and occurrences.</summary>
public sealed partial class EventItem
    : AggregateRoot<EventItemId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Events.EventItem";

    private readonly List<EventCalendarItem> _calendarItems = [];
    private readonly List<EventItemAudience> _audiences = [];
    private readonly List<EventItemOccurrence> _occurrences = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private EventItem() { }

    private EventItem(EventItemId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string? Description { get; private set; }
    public Guid? PhotoBinaryFileId { get; private set; }
    public string? DetailsUrl { get; private set; }
    public EventItemApprovalStatus ApprovalStatus { get; private set; } =
        EventItemApprovalStatus.Approved;
    public Guid? ApprovedByPersonId { get; private set; }
    public DateTimeOffset? ApprovedOnUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

    public IReadOnlyCollection<EventCalendarItem> CalendarItems => _calendarItems.AsReadOnly();
    public IReadOnlyCollection<EventItemAudience> Audiences => _audiences.AsReadOnly();
    public IReadOnlyCollection<EventItemOccurrence> Occurrences => _occurrences.AsReadOnly();

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<EventItem> Create(EventItemSpec spec, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var item = new EventItem(EventItemId.Create());
        var updated = item.Update(spec, nowUtc);
        return updated.IsFailure ? Result.Failure<EventItem>(updated.Errors) : item;
    }

    public Result Update(EventItemSpec spec, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var distinctCalendars = ExtractDistinctCalendars(spec.CalendarIds);
        if (!IsValidItemSpec(spec, distinctCalendars.Length, out var normalizedSlug))
        {
            return EventsErrors.EventItemInvalid;
        }

        Name = spec.Name.Trim();
        Slug = normalizedSlug;
        Summary = TrimOptional(spec.Summary, 500);
        Description = TrimOptional(spec.Description, 4000);
        PhotoBinaryFileId = NormalizeGuid(spec.PhotoBinaryFileId);
        DetailsUrl = TrimOptional(spec.DetailsUrl, 500);
        ApprovalStatus = spec.ApprovalStatus;
        ApprovedByPersonId = NormalizeGuid(spec.ApprovedByPersonId);
        ApprovedOnUtc = ResolveApprovedTimestamp(spec.ApprovalStatus, nowUtc);
        IsActive = spec.IsActive;
        IsPublic = spec.IsPublic;

        SyncCalendars(distinctCalendars);
        SyncAudiences(spec.Audiences);
        ApplyAttributes(spec.Attributes);
        return Result.Success();
    }

    private DateTimeOffset? ResolveApprovedTimestamp(
        EventItemApprovalStatus status,
        DateTimeOffset nowUtc
    )
    {
        return status == EventItemApprovalStatus.Approved
            ? ApprovedOnUtc ?? nowUtc.ToUniversalTime()
            : null;
    }

    private static Guid[] ExtractDistinctCalendars(IReadOnlyList<Guid>? calendarIds)
    {
        return calendarIds is null
            ? []
            : [.. calendarIds.Where(static id => id != Guid.Empty).Distinct()];
    }

    private static bool IsValidItemSpec(
        EventItemSpec spec,
        int calendarCount,
        out string normalizedSlug
    )
    {
        normalizedSlug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        return calendarCount > 0
            && !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 200
            && SlugRegex.IsMatch(normalizedSlug);
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    public Result<EventItemOccurrence> AddOccurrence(EventItemOccurrenceSpec spec)
    {
        var occurrence = new EventItemOccurrence(EventItemOccurrenceId.Create(), Id);
        var applied = occurrence.ApplySpec(spec);
        if (applied.IsFailure)
        {
            return Result.Failure<EventItemOccurrence>(applied.Errors);
        }

        _occurrences.Add(occurrence);
        return occurrence;
    }

    public Result<EventItemOccurrence> UpdateOccurrence(
        EventItemOccurrenceId occurrenceId,
        EventItemOccurrenceSpec spec
    )
    {
        var occurrence = _occurrences.Find(o => o.Id == occurrenceId);
        if (occurrence is null)
        {
            return EventsErrors.OccurrenceNotFound;
        }

        var applied = occurrence.ApplySpec(spec);
        return applied.IsFailure ? Result.Failure<EventItemOccurrence>(applied.Errors) : occurrence;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var changed = false;
        if (ApprovedByPersonId == retiredPersonId)
        {
            ApprovedByPersonId = survivingPersonId;
            changed = true;
        }

        foreach (var occurrence in _occurrences)
        {
            changed |= occurrence.RepointContactPerson(retiredPersonId, survivingPersonId);
        }

        return changed;
    }

    private void SyncCalendars(IReadOnlyList<Guid> calendarIds)
    {
        _ = _calendarItems.RemoveAll(existing => !calendarIds.Contains(existing.CalendarId.Value));
        foreach (var calId in calendarIds)
        {
            if (!_calendarItems.Exists(existing => existing.CalendarId.Value == calId))
            {
                _calendarItems.Add(
                    new EventCalendarItem(
                        EventCalendarItemId.Create(),
                        Id,
                        EventCalendarId.From(calId)
                    )
                );
            }
        }
    }

    private void SyncAudiences(IReadOnlyList<EventItemAudienceDto>? audiences)
    {
        _audiences.Clear();
        if (audiences is null)
        {
            return;
        }

        foreach (var dto in audiences)
        {
            if (!string.IsNullOrWhiteSpace(dto.AudienceLabel))
            {
                var label = TrimOptional(dto.AudienceLabel, 120)!;
                _audiences.Add(
                    new EventItemAudience(
                        EventItemAudienceId.Create(),
                        Id,
                        dto.AudienceDefinedValueId,
                        label
                    )
                );
            }
        }
    }

    private void ApplyAttributes(IReadOnlyDictionary<string, string?>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        _attributeValues.Clear();
        foreach (var (key, value) in attributes)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _attributeValues[key.Trim()] = value;
            }
        }
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
    private static partial Regex SlugRegex { get; }
}
