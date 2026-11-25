using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Records a volunteer Accept/Decline/Maybe RSVP via AttendanceId or signed RSVP token.</summary>
public sealed record RespondToRsvpCommand(
    Guid? AttendanceId,
    string? RsvpToken,
    RsvpStatus Response,
    Guid? DeclineReasonValueId = null,
    string? DeclineReasonNote = null
) : ICommand<ScheduledVolunteerResponse>;
