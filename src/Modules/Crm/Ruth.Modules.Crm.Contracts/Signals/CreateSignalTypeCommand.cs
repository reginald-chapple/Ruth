using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Command to create a new signal type.</summary>
public sealed record CreateSignalTypeCommand(
    string Name,
    string SignalColor,
    string? SignalIcon = null,
    string? Description = null,
    int Order = 0
) : ICommand<SignalTypeResponse>;
