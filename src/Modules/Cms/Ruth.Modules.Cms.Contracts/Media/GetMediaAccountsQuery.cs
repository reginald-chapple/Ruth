using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Query to list configured CMS media accounts.</summary>
public sealed record GetMediaAccountsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<MediaAccountResponse>>;
