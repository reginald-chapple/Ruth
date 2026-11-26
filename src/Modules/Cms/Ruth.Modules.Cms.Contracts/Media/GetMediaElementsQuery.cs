using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Query to list media elements optionally filtered by media folder or media account.</summary>
public sealed record GetMediaElementsQuery(Guid? MediaFolderId = null, Guid? MediaAccountId = null)
    : IQuery<IReadOnlyList<MediaElementResponse>>;
