namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Represents a configured external OpenID Connect identity provider.</summary>
public sealed record ExternalAuthProviderResponse(
    Guid Id,
    string ProviderKey,
    string DisplayName,
    string Authority,
    string ClientId,
    string Scopes,
    bool IsEnabled,
    bool AutoLinkByEmail,
    string CallbackPath
);
