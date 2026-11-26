using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Query to list media folders optionally filtered by media account.</summary>
public sealed record GetMediaFoldersQuery(Guid? MediaAccountId = null)
    : IQuery<IReadOnlyList<MediaFolderResponse>>;
