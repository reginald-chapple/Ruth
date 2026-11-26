using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Query to list CMS content channel types.</summary>
public sealed record GetContentChannelTypesQuery
    : IQuery<IReadOnlyList<ContentChannelTypeResponse>>;
