using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Creates or updates an external OIDC authentication provider configuration.</summary>
public sealed record UpsertExternalAuthProviderCommand(
    string ProviderKey,
    string DisplayName,
    string Authority,
    string ClientId,
    string? ClientSecret,
    string Scopes,
    bool IsEnabled,
    bool AutoLinkByEmail,
    string CallbackPath
) : ICommand<ExternalAuthProviderResponse>;
