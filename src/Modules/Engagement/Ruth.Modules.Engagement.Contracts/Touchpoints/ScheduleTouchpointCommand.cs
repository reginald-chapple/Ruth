using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Command to schedule an ad-hoc or follow-up Touchpoint for a ConstituentContact.</summary>
/// <param name="ConstituentContactId">Target ConstituentContact identifier.</param>
/// <param name="Kind">Touchpoint kind.</param>
/// <param name="Channel">Outreach channel.</param>
/// <param name="ScheduledDate">Scheduled calendar date.</param>
/// <param name="Summary">Optional prompt/context note.</param>
public sealed record ScheduleTouchpointCommand(
    Guid ConstituentContactId,
    TouchpointKind Kind,
    TouchpointChannel Channel,
    DateOnly ScheduledDate,
    string? Summary = null
) : ICommand<TouchpointResponse>;
