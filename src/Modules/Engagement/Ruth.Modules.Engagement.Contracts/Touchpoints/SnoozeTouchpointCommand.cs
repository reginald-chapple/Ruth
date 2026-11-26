using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Command to snooze a pending Touchpoint to a future date.</summary>
/// <param name="TouchpointId">Target Touchpoint identifier.</param>
/// <param name="SnoozeUntilDate">New scheduled date.</param>
/// <param name="Note">Optional snooze note.</param>
public sealed record SnoozeTouchpointCommand(
    Guid TouchpointId,
    DateOnly SnoozeUntilDate,
    string? Note = null
) : ICommand<TouchpointResponse>;
