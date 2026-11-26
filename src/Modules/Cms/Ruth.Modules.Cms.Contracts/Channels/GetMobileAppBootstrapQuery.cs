using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Query to build the versioned mobile shell bootstrap payload.</summary>
public sealed record GetMobileAppBootstrapQuery(string AppSlug, string? ClientVersion = null)
    : IQuery<MobileAppBootstrapResponse>;
