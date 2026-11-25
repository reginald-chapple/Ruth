using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Tags;

/// <summary>Gets all tags currently applied to a specific entity (visible to the viewer).</summary>
public sealed record GetEntityTagsQuery(Guid EntityTypeId, Guid EntityId, Guid? ViewerUserId = null)
    : IQuery<IReadOnlyList<TagResponse>>;
