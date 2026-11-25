using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Completes an external OIDC callback by validating session state, exchanging the authorization code, and issuing Ruth tokens.</summary>
public sealed record CompleteOidcLoginCommand(
    string ProviderKey,
    string State,
    string Code,
    string? IpAddress = null,
    string? UserAgent = null
) : ICommand<AuthTokensResponse>;
