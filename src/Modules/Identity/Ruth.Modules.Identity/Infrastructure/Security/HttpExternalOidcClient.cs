using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.JsonWebTokens;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed class HttpExternalOidcClient(IHttpClientFactory httpClientFactory)
    : IExternalOidcClient
{
    public const string HttpClientName = "RuthExternalOidc";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory
        ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public async Task<Result<string>> BuildAuthorizationUrlAsync(
        ExternalAuthProviderConfig provider,
        string state,
        string nonce,
        string codeChallenge,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(provider);

        var discovery = await FetchDiscoveryAsync(provider.Authority, cancellationToken);
        var authorizeEndpoint = discovery?.AuthorizationEndpoint
            ?? $"{provider.Authority}/authorize";

        var query = string.Join(
            '&',
            $"client_id={Uri.EscapeDataString(provider.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(provider.CallbackPath)}",
            "response_type=code",
            $"scope={Uri.EscapeDataString(provider.Scopes)}",
            $"state={Uri.EscapeDataString(state)}",
            $"nonce={Uri.EscapeDataString(nonce)}",
            $"code_challenge={Uri.EscapeDataString(codeChallenge)}",
            "code_challenge_method=S256"
        );

        var separator = authorizeEndpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{authorizeEndpoint}{separator}{query}";
    }

    public async Task<Result<ExternalOidcUserInfo>> ExchangeCodeAsync(
        ExternalAuthProviderConfig provider,
        string code,
        string codeVerifier,
        string expectedNonce,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(provider);

        var discovery = await FetchDiscoveryAsync(provider.Authority, cancellationToken);
        var tokenEndpoint = discovery?.TokenEndpoint ?? $"{provider.Authority}/oauth/token";

        var formFields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = provider.CallbackPath,
            ["client_id"] = provider.ClientId,
            ["code_verifier"] = codeVerifier,
        };
        if (!string.IsNullOrWhiteSpace(provider.ClientSecret))
        {
            formFields["client_secret"] = provider.ClientSecret;
        }

        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var form = new FormUrlEncodedContent(formFields);
        using var response = await client.PostAsync(
            new Uri(tokenEndpoint, UriKind.Absolute),
            form,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var tokenPayload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return ExtractUserInfo(tokenPayload, expectedNonce);
    }

    private async Task<OidcDiscoveryDocument?> FetchDiscoveryAsync(
        string authority,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            var discoveryUri = new Uri(
                $"{authority.TrimEnd('/')}/.well-known/openid-configuration",
                UriKind.Absolute
            );
            using var response = await client.GetAsync(discoveryUri, cancellationToken);
            return !response.IsSuccessStatusCode
                ? null
                : await response.Content.ReadFromJsonAsync<OidcDiscoveryDocument>(
                    cancellationToken
                );
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static Result<ExternalOidcUserInfo> ExtractUserInfo(
        JsonElement tokenPayload,
        string expectedNonce
    )
    {
        if (
            !tokenPayload.TryGetProperty("id_token", out var idTokenProp)
            || idTokenProp.GetString() is not { Length: > 0 } idToken
        )
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var handler = new JsonWebTokenHandler();
        if (!handler.CanReadToken(idToken))
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var jwt = handler.ReadJsonWebToken(idToken);
        var tokenNonce = jwt
            .Claims.FirstOrDefault(static c =>
                string.Equals(c.Type, "nonce", StringComparison.Ordinal)
            )
            ?.Value;
        if (
            !string.IsNullOrEmpty(expectedNonce)
            && !string.Equals(tokenNonce, expectedNonce, StringComparison.Ordinal)
        )
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var subject = jwt.Subject;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var email = jwt
            .Claims.FirstOrDefault(static c =>
                string.Equals(c.Type, "email", StringComparison.Ordinal)
            )
            ?.Value;
        var emailVerifiedRaw = jwt
            .Claims.FirstOrDefault(static c =>
                string.Equals(c.Type, "email_verified", StringComparison.Ordinal)
            )
            ?.Value;
        var emailVerified = string.Equals(
            emailVerifiedRaw,
            "true",
            StringComparison.OrdinalIgnoreCase
        );
        var name = jwt
            .Claims.FirstOrDefault(static c =>
                string.Equals(c.Type, "name", StringComparison.Ordinal)
            )
            ?.Value;

        return new ExternalOidcUserInfo(subject, email, emailVerified, name);
    }

    private sealed record OidcDiscoveryDocument(
        [property: JsonPropertyName("authorization_endpoint")] string? AuthorizationEndpoint,
        [property: JsonPropertyName("token_endpoint")] string? TokenEndpoint
    );
}
