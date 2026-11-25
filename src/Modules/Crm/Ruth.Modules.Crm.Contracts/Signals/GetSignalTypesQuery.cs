using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Signals;

/// <summary>Query to list all configured signal types ordered by priority.</summary>
public sealed record GetSignalTypesQuery : IQuery<IReadOnlyList<SignalTypeResponse>>;
