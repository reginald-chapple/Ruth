using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Tags;

/// <summary>Lists personal (owned by viewer) and organizational tags matching optional entity type and search prefix.</summary>
public sealed record GetTagsQuery(
    Guid? EntityTypeId = null,
    Guid? ViewerUserId = null,
    string? SearchTerm = null,
    bool ActiveOnly = true
) : IQuery<IReadOnlyList<TagResponse>>;
