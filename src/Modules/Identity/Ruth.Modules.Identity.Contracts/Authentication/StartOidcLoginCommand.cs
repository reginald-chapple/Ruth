using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Initiates a PKCE-protected external OIDC authentication session.</summary>
public sealed record StartOidcLoginCommand(string ProviderKey, string? ReturnUrl = null)
    : ICommand<StartOidcLoginResponse>;
