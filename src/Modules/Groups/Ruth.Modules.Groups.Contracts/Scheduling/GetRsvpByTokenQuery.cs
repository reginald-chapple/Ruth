using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Looks up a scheduled RSVP item by its RSVP link token.</summary>
public sealed record GetRsvpByTokenQuery(string RsvpToken) : IQuery<ScheduledVolunteerResponse>;
