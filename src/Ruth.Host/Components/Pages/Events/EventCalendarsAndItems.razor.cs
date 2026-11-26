using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Events;

/// <summary>Staff workspace for managing event calendars, occurrences, and RFC 5545 iCalendar feeds.</summary>
public sealed partial class EventCalendarsAndItems : ComponentBase
{
    private List<EventCalendarResponse> _calendars = [];
    private List<EventItemResponse> _items = [];

    private string _calendarName = "Community Workshops";
    private string _calendarSlug = "community-workshops";
    private string _calendarDescription = "Workshops, town halls, and training sessions.";

    private Guid _selectedCalendarId;
    private string _eventName = "Annual Community Town Hall";
    private string _eventSlug = "annual-town-hall-2026";
    private string _audienceLabel = "All Members";
    private string _locationDescription = "Main Auditorium";
    private string _startIsoUtc = DateTimeOffset
        .UtcNow.AddDays(7)
        .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var calRes = await QueryDispatcher.QueryAsync<
            GetEventCalendarsQuery,
            IReadOnlyList<EventCalendarResponse>
        >(new GetEventCalendarsQuery());
        if (calRes.IsSuccess)
        {
            _calendars = [.. calRes.Value];
            if (_selectedCalendarId == Guid.Empty && _calendars.Count > 0)
            {
                _selectedCalendarId = _calendars[0].Id;
            }
        }

        var itemsRes = await QueryDispatcher.QueryAsync<
            GetEventItemsQuery,
            IReadOnlyList<EventItemResponse>
        >(new GetEventItemsQuery());
        if (itemsRes.IsSuccess)
        {
            _items = [.. itemsRes.Value];
        }
    }

    private async Task CreateCalendarAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateEventCalendarCommand,
            EventCalendarResponse
        >(
            new CreateEventCalendarCommand(
                new EventCalendarSpec(_calendarName, _calendarSlug, _calendarDescription)
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Created calendar {res.Value.Name}.";
        await ReloadAsync();
    }

    private async Task CreateEventWithOccurrenceAsync()
    {
        if (
            !DateTimeOffset.TryParse(
                _startIsoUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var startUtc
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter a valid ISO-8601 UTC start timestamp.";
            return;
        }

        var itemRes = await CommandDispatcher.SendAsync<CreateEventItemCommand, EventItemResponse>(
            new CreateEventItemCommand(
                new EventItemSpec(
                    _eventName,
                    _eventSlug,
                    [_selectedCalendarId],
                    Audiences:
                    [
                        new EventItemAudienceDto(AudienceDefinedValueId: null, _audienceLabel),
                    ]
                )
            )
        );
        if (itemRes.IsFailure)
        {
            _isError = true;
            _statusMessage = itemRes.PrimaryError.Description;
            return;
        }

        var occRes = await CommandDispatcher.SendAsync<
            AddEventItemOccurrenceCommand,
            EventItemOccurrenceResponse
        >(
            new AddEventItemOccurrenceCommand(
                itemRes.Value.Id,
                new EventItemOccurrenceSpec(
                    LocationDescription: _locationDescription,
                    CustomStartUtc: startUtc,
                    CustomEndUtc: startUtc.AddHours(2)
                )
            )
        );
        if (occRes.IsFailure)
        {
            _isError = true;
            _statusMessage = occRes.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Published {itemRes.Value.Name} with occurrence.";
        await ReloadAsync();
    }
}
