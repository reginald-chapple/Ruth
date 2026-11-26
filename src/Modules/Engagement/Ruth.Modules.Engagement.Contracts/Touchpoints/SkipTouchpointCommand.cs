using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Command to skip a pending Touchpoint and roll cadence forward to the next cycle.</summary>
/// <param name="TouchpointId">Target Touchpoint identifier.</param>
/// <param name="Reason">Optional skip reason.</param>
public sealed record SkipTouchpointCommand(Guid TouchpointId, string? Reason = null)
    : ICommand<TouchpointResponse>;
