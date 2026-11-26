using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Query to list configured mobile applications.</summary>
public sealed record GetMobileApplicationsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<MobileApplicationResponse>>;
