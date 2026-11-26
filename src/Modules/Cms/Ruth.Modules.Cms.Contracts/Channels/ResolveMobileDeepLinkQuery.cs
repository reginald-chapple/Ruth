using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Query to resolve a deep link path for a mobile application.</summary>
public sealed record ResolveMobileDeepLinkQuery(string AppSlug, string Path)
    : IQuery<ResolvedMobileDeepLinkResponse>;
