using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Query to generate a Digital Signage, Apple TV, or Roku JSON feed for an application.</summary>
public sealed record GetSignageFeedQuery(
    string AppSlug,
    string Format = "signage",
    string BaseOrigin = "https://ruth.local"
) : IQuery<SignageFeedResponse>;
