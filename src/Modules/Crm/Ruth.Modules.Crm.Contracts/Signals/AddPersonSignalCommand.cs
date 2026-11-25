using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Command to attach a signal to a person and immediately recalculate TopSignal.</summary>
public sealed record AddPersonSignalCommand(
    Guid PersonId,
    Guid SignalTypeId,
    Guid OwnerPersonId,
    string? Note = null,
    DateTimeOffset? ExpirationDateUtc = null
) : ICommand<PersonSignalResponse>;
