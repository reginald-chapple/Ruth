using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Lists DefinedTypes optionally filtered by category or search term.</summary>
public sealed record GetDefinedTypesQuery(
    Guid? CategoryId = null,
    string? SearchTerm = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<DefinedTypeSummaryResponse>>;
