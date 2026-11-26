using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain;

/// <summary>Domain errors for the Events bounded context.</summary>
public static class EventsErrors
{
    public static readonly DomainError CalendarInvalid = DomainError.Validation(
        "Events.Calendar.Invalid",
        "Event calendar requires a non-empty name (<= 160 chars) and valid URL slug."
    );

    public static readonly DomainError CalendarSlugConflict = DomainError.Conflict(
        "Events.Calendar.SlugConflict",
        "Another event calendar already uses this slug."
    );

    public static readonly DomainError CalendarNotFound = DomainError.NotFound(
        "Events.Calendar.NotFound",
        "The requested event calendar was not found."
    );

    public static readonly DomainError EventItemInvalid = DomainError.Validation(
        "Events.EventItem.Invalid",
        "Event item requires a non-empty name, valid URL slug, and at least one calendar."
    );

    public static readonly DomainError EventItemSlugConflict = DomainError.Conflict(
        "Events.EventItem.SlugConflict",
        "Another event item already uses this slug."
    );

    public static readonly DomainError EventItemNotFound = DomainError.NotFound(
        "Events.EventItem.NotFound",
        "The requested event item was not found."
    );

    public static readonly DomainError OccurrenceInvalid = DomainError.Validation(
        "Events.Occurrence.Invalid",
        "An event item occurrence requires either a ScheduleId or a valid CustomStartUtc window."
    );

    public static readonly DomainError OccurrenceNotFound = DomainError.NotFound(
        "Events.Occurrence.NotFound",
        "The requested event item occurrence was not found."
    );

    public static readonly DomainError InteractiveSessionInvalid = DomainError.Validation(
        "Events.InteractiveSession.Invalid",
        "Interactive session requires a non-empty title (<= 200 chars)."
    );

    public static readonly DomainError InteractiveSessionNotFound = DomainError.NotFound(
        "Events.InteractiveSession.NotFound",
        "The requested interactive session was not found."
    );

    public static readonly DomainError InteractiveSessionNotLive = DomainError.Conflict(
        "Events.InteractiveSession.NotLive",
        "Audience responses can only be submitted while the interactive session is live."
    );

    public static readonly DomainError InteractiveActionInvalid = DomainError.Validation(
        "Events.InteractiveAction.Invalid",
        "Interactive action requires a title, prompt text, and at least two distinct options for multiple-choice polls."
    );

    public static readonly DomainError InteractiveActionNotFound = DomainError.NotFound(
        "Events.InteractiveAction.NotFound",
        "The requested interactive action was not found or is closed."
    );

    public static readonly DomainError InteractiveDuplicateResponse = DomainError.Conflict(
        "Events.InteractiveResponse.Duplicate",
        "This participant has already submitted a response to this prompt."
    );

    public static readonly DomainError InteractiveResponseInvalid = DomainError.Validation(
        "Events.InteractiveResponse.Invalid",
        "Participant token and a valid option or text response are required."
    );

    public static readonly DomainError InteractiveResponseNotFound = DomainError.NotFound(
        "Events.InteractiveResponse.NotFound",
        "The requested interactive response was not found."
    );
}
