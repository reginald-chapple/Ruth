using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Command to update a signal type and recalculate affected persons' denormalized TopSignal fields.</summary>
public sealed record UpdateSignalTypeCommand(
    Guid SignalTypeId,
    string Name,
    string SignalColor,
    string? SignalIcon = null,
    string? Description = null,
    int Order = 0
) : ICommand<SignalTypeResponse>;
