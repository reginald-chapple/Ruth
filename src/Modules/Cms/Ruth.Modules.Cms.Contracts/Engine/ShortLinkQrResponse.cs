namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Result of resolving a shortlink token including its target URL and QR SVG markup.</summary>
public sealed record ShortLinkQrResponse(
    Guid ShortLinkId,
    string Token,
    string ShortUrl,
    string TargetUrl,
    string QrSvgMarkup
);
