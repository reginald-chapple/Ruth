using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Query to list configured Digital Signage / TV applications.</summary>
public sealed record GetSignageApplicationsQuery(
    SignagePlatformType? Platform = null,
    bool IncludeInactive = false
) : IQuery<IReadOnlyList<SignageApplicationResponse>>;
