using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class GetCalendarIcsFeedQueryHandler(
    EventsDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    TimeProvider timeProvider
) : IQueryHandler<GetCalendarIcsFeedQuery, CalendarFeedPayloadResponse>
{
    public async Task<Result<CalendarFeedPayloadResponse>> HandleAsync(
        GetCalendarIcsFeedQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var calendar = await ResolveCalendarAsync(query.CalendarIdOrSlug, cancellationToken);
        if (calendar is null || !calendar.IsActive)
        {
            return EventsErrors.CalendarNotFound;
        }

        var nowUtc = timeProvider.GetUtcNow();
        var rangeStart = (query.RangeStartUtc ?? nowUtc.AddDays(-30)).ToUniversalTime();
        var rangeEnd = (query.RangeEndUtc ?? nowUtc.AddDays(180)).ToUniversalTime();
        if (rangeEnd < rangeStart)
        {
            rangeEnd = rangeStart.AddDays(30);
        }

        var items = await LoadEligibleEventItemsAsync(calendar.Id, query, cancellationToken);
        var entries = await ExpandFeedEntriesAsync(
            items,
            query.BranchId,
            rangeStart,
            rangeEnd,
            nowUtc,
            cancellationToken
        );

        var ics = IcsCalendarFeedBuilder.BuildFeed(calendar.Name, calendar.Description, entries);
        return new CalendarFeedPayloadResponse(calendar.Slug, "text/calendar; charset=utf-8", ics);
    }

    private Task<EventCalendar?> ResolveCalendarAsync(
        string slugOrId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(slugOrId))
        {
            return Task.FromResult<EventCalendar?>(null);
        }

        if (Guid.TryParse(slugOrId.Trim(), out var guid))
        {
            var id = EventCalendarId.From(guid);
            return dbContext
                .EventCalendars.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        var slug = slugOrId.Trim().ToLowerInvariant();
        return dbContext
            .EventCalendars.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    private Task<List<EventItem>> LoadEligibleEventItemsAsync(
        EventCalendarId calendarId,
        GetCalendarIcsFeedQuery query,
        CancellationToken cancellationToken
    )
    {
        var q = dbContext
            .EventItems.AsNoTracking()
            .Include(static i => i.CalendarItems)
            .Include(static i => i.Audiences)
            .Include(static i => i.Occurrences)
            .Where(i =>
                i.IsActive
                && i.ApprovalStatus == EventItemApprovalStatus.Approved
                && i.CalendarItems.Any(c => c.CalendarId == calendarId)
            );

        if (query.AudienceDefinedValueId is { } audId && audId != Guid.Empty)
        {
            q = q.Where(i => i.Audiences.Any(a => a.AudienceDefinedValueId == audId));
        }

        return q.ToListAsync(cancellationToken);
    }

    private async Task<List<IcsCalendarEventEntry>> ExpandFeedEntriesAsync(
        List<EventItem> items,
        Guid? branchFilter,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        DateTimeOffset stampUtc,
        CancellationToken cancellationToken
    )
    {
        var entries = new List<IcsCalendarEventEntry>();
        var locationCache = new Dictionary<Guid, string?>();

        foreach (var item in items)
        {
            var categories = item.Audiences.Select(static a => a.AudienceLabel).ToArray();
            foreach (var occ in item.Occurrences)
            {
                if (branchFilter is { } bf && bf != Guid.Empty && occ.BranchId != bf)
                {
                    continue;
                }

                var locationLabel = await ResolveLocationLabelAsync(
                    occ,
                    locationCache,
                    cancellationToken
                );
                await AppendOccurrenceEntriesAsync(
                    entries,
                    item,
                    occ,
                    locationLabel,
                    categories,
                    rangeStart,
                    rangeEnd,
                    stampUtc,
                    cancellationToken
                );
            }
        }

        return entries;
    }

    private async Task AppendOccurrenceEntriesAsync(
        List<IcsCalendarEventEntry> entries,
        EventItem item,
        EventItemOccurrence occ,
        string? locationLabel,
        string[] categories,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        DateTimeOffset stampUtc,
        CancellationToken cancellationToken
    )
    {
        var description = item.Description ?? item.Summary ?? occ.OccurrenceNote;
        AppendCustomWindowEntry(
            entries,
            item,
            occ,
            description,
            locationLabel,
            categories,
            rangeStart,
            rangeEnd,
            stampUtc
        );

        if (occ.ScheduleId is not { } scheduleId)
        {
            return;
        }

        var schedRes = await queryDispatcher.QueryAsync<
            GetScheduleOccurrencesQuery,
            IReadOnlyList<ScheduleOccurrenceDto>
        >(
            new GetScheduleOccurrencesQuery(scheduleId, rangeStart, rangeEnd, occ.TimeZoneId),
            cancellationToken
        );
        if (schedRes.IsFailure)
        {
            return;
        }

        foreach (var slot in schedRes.Value)
        {
            var uid = string.Create(
                CultureInfo.InvariantCulture,
                $"{occ.Id.Value:D}-{slot.StartOnUtc.ToUnixTimeSeconds()}@calendar.events"
            );
            entries.Add(
                new IcsCalendarEventEntry(
                    uid,
                    item.Name,
                    slot.StartOnUtc,
                    slot.EndOnUtc,
                    stampUtc,
                    description,
                    locationLabel,
                    item.DetailsUrl,
                    categories,
                    occ.IsCancelled
                )
            );
        }
    }

    private static void AppendCustomWindowEntry(
        List<IcsCalendarEventEntry> entries,
        EventItem item,
        EventItemOccurrence occ,
        string? description,
        string? locationLabel,
        string[] categories,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        DateTimeOffset stampUtc
    )
    {
        if (occ.CustomStartUtc is not { } startUtc)
        {
            return;
        }

        var endUtc = occ.CustomEndUtc ?? startUtc.AddHours(1);
        if (endUtc >= rangeStart && startUtc <= rangeEnd)
        {
            entries.Add(
                new IcsCalendarEventEntry(
                    $"{occ.Id.Value:D}@calendar.events",
                    item.Name,
                    startUtc,
                    endUtc,
                    stampUtc,
                    description,
                    locationLabel,
                    item.DetailsUrl,
                    categories,
                    occ.IsCancelled
                )
            );
        }
    }

    private async Task<string?> ResolveLocationLabelAsync(
        EventItemOccurrence occ,
        Dictionary<Guid, string?> cache,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(occ.LocationDescription))
        {
            return occ.LocationDescription;
        }

        if (occ.LocationId is not { } locId)
        {
            return null;
        }

        if (cache.TryGetValue(locId, out var cached))
        {
            return cached;
        }

        var locRes = await queryDispatcher.QueryAsync<GetLocationByIdQuery, LocationResponse>(
            new GetLocationByIdQuery(locId),
            cancellationToken
        );
        var resolved = locRes.IsSuccess ? locRes.Value.DisplayName : null;
        cache[locId] = resolved;
        return resolved;
    }
}
