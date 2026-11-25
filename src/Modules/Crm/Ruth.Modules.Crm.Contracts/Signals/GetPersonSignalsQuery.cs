using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Query to list signals attached to a person.</summary>
public sealed record GetPersonSignalsQuery(Guid PersonId, bool IncludeExpired = false)
    : IQuery<IReadOnlyList<PersonSignalResponse>>;
