using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Apple PassKit WebService v1 query fetching the latest .pkpass bundle for an authenticated device.</summary>
/// <param name="PassTypeIdentifier">PassKit passTypeIdentifier path parameter.</param>
/// <param name="SerialNumber">Issued pass serial number path parameter.</param>
/// <param name="ApplePassAuthToken">Raw token extracted from Authorization: ApplePass header.</param>
/// <param name="IfModifiedSinceUtc">Optional If-Modified-Since HTTP header timestamp.</param>
/// <param name="PublicBaseUrl">Optional public base URL used to populate webServiceURL in pass.json.</param>
public sealed record GetPassKitLatestPassQuery(
    string PassTypeIdentifier,
    string SerialNumber,
    string? ApplePassAuthToken,
    DateTimeOffset? IfModifiedSinceUtc = null,
    string? PublicBaseUrl = null
) : IQuery<PassKitLatestPassResponse>;
