namespace Ruth.Modules.Tenancy.Slices.Endpoints;

/// <summary>Request payload for verifying custom domain DNS TXT ownership.</summary>
/// <param name="ObservedTxtToken">Optional simulated TXT token value honored strictly when DNS mock mode is enabled.</param>
public sealed record VerifyDomainTokenRequest(string? ObservedTxtToken = null);
