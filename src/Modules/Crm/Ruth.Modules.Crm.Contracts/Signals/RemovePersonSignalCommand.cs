using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Command to remove a person signal and recalculate TopSignal.</summary>
public sealed record RemovePersonSignalCommand(Guid PersonSignalId) : ICommand<bool>;
