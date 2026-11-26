namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Apple PassKit WebService v1 response carrying either a 304 Not Modified flag or the latest signed .pkpass archive.</summary>
/// <param name="NotModified">True when If-Modified-Since is at or after the pass's last update second.</param>
/// <param name="LastModifiedUtc">UTC timestamp truncated to whole seconds for HTTP Last-Modified headers.</param>
/// <param name="PkpassBytes">Signed .pkpass archive bytes (or deterministic zip fallback in unconfigured dev/test environments).</param>
public sealed record PassKitLatestPassResponse(
    bool NotModified,
    DateTimeOffset LastModifiedUtc,
    byte[] PkpassBytes
);
