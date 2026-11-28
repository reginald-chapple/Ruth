namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Masked response describing a per-tenant communication transport credential configuration.</summary>
/// <param name="Id">Configuration aggregate identifier.</param>
/// <param name="TransportKey">Canonical lowercase transport key.</param>
/// <param name="Channel">Communication medium channel.</param>
/// <param name="IsEnabled">Whether this tenant transport configuration is enabled.</param>
/// <param name="DefaultFromAddress">Optional sender email or E.164 phone number override.</param>
/// <param name="DefaultFromName">Optional sender display name override.</param>
/// <param name="MaskedCredentials">Credential keys with secret values redacted as ********.</param>
/// <param name="CreatedOnUtc">Creation timestamp (UTC).</param>
/// <param name="ModifiedOnUtc">Last modification timestamp (UTC).</param>
public sealed record CommunicationTransportConfigResponse(
    Guid Id,
    string TransportKey,
    CommunicationChannel Channel,
    bool IsEnabled,
    string? DefaultFromAddress,
    string? DefaultFromName,
    IReadOnlyDictionary<string, string> MaskedCredentials,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
