using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.History;

/// <summary>Queries the structured history timeline for an entity or entity type.</summary>
public sealed record GetHistoryLogQuery(
    Guid? EntityTypeId = null,
    Guid? EntityId = null,
    Guid? CategoryId = null,
    int MaxResults = 100
) : IQuery<IReadOnlyList<HistoryLogEntryResponse>>;
