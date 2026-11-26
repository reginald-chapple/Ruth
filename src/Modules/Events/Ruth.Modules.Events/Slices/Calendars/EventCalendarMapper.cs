using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain.Calendars;

namespace Ruth.Modules.Events.Slices.Calendars;

internal static class EventCalendarMapper
{
    public static EventCalendarResponse MapCalendar(EventCalendar calendar)
    {
        return new EventCalendarResponse(
            calendar.Id.Value,
            calendar.Name,
            calendar.Slug,
            calendar.Description,
            calendar.IconCssClass,
            calendar.ColorHex,
            calendar.IsPublic,
            calendar.IsIndexEnabled,
            calendar.IsActive,
            calendar.CreatedOnUtc,
            calendar.ModifiedOnUtc
        );
    }

    public static EventItemResponse MapEventItem(EventItem item)
    {
        return new EventItemResponse(
            item.Id.Value,
            item.Name,
            item.Slug,
            item.Summary,
            item.Description,
            item.PhotoBinaryFileId,
            item.DetailsUrl,
            item.ApprovalStatus,
            item.ApprovedByPersonId,
            item.ApprovedOnUtc,
            item.IsActive,
            item.IsPublic,
            [.. item.CalendarItems.Select(static c => c.CalendarId.Value)],
            [
                .. item.Audiences.Select(static a => new EventItemAudienceDto(
                    a.AudienceDefinedValueId,
                    a.AudienceLabel
                )),
            ],
            [.. item.Occurrences.Select(MapOccurrence)],
            item.AttributeValues,
            item.CreatedOnUtc,
            item.ModifiedOnUtc
        );
    }

    public static EventItemOccurrenceResponse MapOccurrence(EventItemOccurrence occurrence)
    {
        return new EventItemOccurrenceResponse(
            occurrence.Id.Value,
            occurrence.EventItemId.Value,
            occurrence.BranchId,
            occurrence.LocationId,
            occurrence.LocationDescription,
            occurrence.ScheduleId,
            occurrence.CustomStartUtc,
            occurrence.CustomEndUtc,
            occurrence.TimeZoneId,
            occurrence.ContactPersonId,
            occurrence.ContactPhone,
            occurrence.ContactEmail,
            occurrence.OccurrenceNote,
            occurrence.RegistrationInstanceId,
            occurrence.IsCancelled
        );
    }
}
