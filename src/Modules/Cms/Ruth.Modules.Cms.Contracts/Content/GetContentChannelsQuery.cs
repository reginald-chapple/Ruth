using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Query to list CMS content channels.</summary>
public sealed record GetContentChannelsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ContentChannelResponse>>;
