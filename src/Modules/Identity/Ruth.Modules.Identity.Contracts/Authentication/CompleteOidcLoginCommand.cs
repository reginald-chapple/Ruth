using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Completes an external OIDC callback by validating session state, redirect URI, PKCE, and nonce before issuing tokens.</summary>
public sealed record CompleteOidcLoginCommand(
    string ProviderKey,
    string State,
    string Code,
    string? IpAddress = null,
    string? UserAgent = null,
    string? RedirectUri = null
) : ICommand<AuthTokensResponse>;
