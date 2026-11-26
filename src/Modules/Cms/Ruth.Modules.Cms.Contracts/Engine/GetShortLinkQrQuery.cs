using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Query to generate a deterministic scannable QR code SVG for a CMS shortlink.</summary>
public sealed record GetShortLinkQrQuery(string Token, string BaseOrigin = "https://ruth.local")
    : IQuery<ShortLinkQrResponse>;
