using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Abstraction for external OpenID Connect authorization URL construction and code exchange.</summary>
public interface IExternalOidcClient
{
    /// <summary>Builds the provider authorization redirect URL using PKCE S256, state, and nonce.</summary>
    Task<Result<string>> BuildAuthorizationUrlAsync(
        ExternalAuthProviderConfig provider,
        string state,
        string nonce,
        string codeChallenge,
        CancellationToken cancellationToken = default
    );

    /// <summary>Exchanges an authorization code for verified external user identity claims.</summary>
    Task<Result<ExternalOidcUserInfo>> ExchangeCodeAsync(
        ExternalAuthProviderConfig provider,
        string code,
        string codeVerifier,
        string expectedNonce,
        CancellationToken cancellationToken = default
    );
}
