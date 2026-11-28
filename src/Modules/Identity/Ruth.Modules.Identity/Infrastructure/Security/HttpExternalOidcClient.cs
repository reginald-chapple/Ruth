using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.JsonWebTokens;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

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
        if (
            string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(nonce)
            || string.IsNullOrWhiteSpace(codeChallenge)
        )
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        try
        {
            var authorityUri = new Uri(provider.Authority, UriKind.Absolute);
            OutboundAddressGuard.EnsureAllowedUri(authorityUri);

            var discovery = await FetchDiscoveryAsync(provider.Authority, cancellationToken);
            var authorizeEndpoint = discovery?.AuthorizationEndpoint
                ?? $"{provider.Authority}/authorize";
            OutboundAddressGuard.EnsureAllowedUri(new Uri(authorizeEndpoint, UriKind.Absolute));

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
        catch (OutboundSsrfBlockedException)
        {
            return IdentityErrors.ExternalProviderSsrfBlocked;
        }
        catch (UriFormatException)
        {
            return IdentityErrors.ExternalProviderInvalid;
        }
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
        if (
            string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(codeVerifier)
            || string.IsNullOrWhiteSpace(expectedNonce)
        )
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        try
        {
            OutboundAddressGuard.EnsureAllowedUri(new Uri(provider.Authority, UriKind.Absolute));
            var discovery = await FetchDiscoveryAsync(provider.Authority, cancellationToken);
            var tokenEndpoint = discovery?.TokenEndpoint ?? $"{provider.Authority}/oauth/token";
            var tokenUri = new Uri(tokenEndpoint, UriKind.Absolute);
            OutboundAddressGuard.EnsureAllowedUri(tokenUri);

            var formFields = BuildTokenExchangeFields(provider, code, codeVerifier);
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var form = new FormUrlEncodedContent(formFields);
            using var response = await client.PostAsync(tokenUri, form, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return IdentityErrors.RemoteAuthSessionInvalid;
            }

            var tokenPayload = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken
            );
            return ExtractUserInfo(tokenPayload, expectedNonce);
        }
        catch (OutboundSsrfBlockedException)
        {
            return IdentityErrors.ExternalProviderSsrfBlocked;
        }
        catch (Exception ex) when (ex is HttpRequestException or UriFormatException)
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }
    }

    private static Dictionary<string, string> BuildTokenExchangeFields(
        ExternalAuthProviderConfig provider,
        string code,
        string codeVerifier
    )
    {
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

        return formFields;
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
            OutboundAddressGuard.EnsureAllowedUri(discoveryUri);
            using var response = await client.GetAsync(discoveryUri, cancellationToken);
            return !response.IsSuccessStatusCode
                ? null
                : await response.Content.ReadFromJsonAsync<OidcDiscoveryDocument>(
                    cancellationToken
                );
        }
        catch (OutboundSsrfBlockedException)
        {
            throw;
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
        if (!IsValidSignedIdTokenWithNonce(jwt, expectedNonce))
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var subject = jwt.Subject;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var email = FindClaimValue(jwt, "email");
        var emailVerified = string.Equals(
            FindClaimValue(jwt, "email_verified"),
            "true",
            StringComparison.OrdinalIgnoreCase
        );
        var name = FindClaimValue(jwt, "name");

        return new ExternalOidcUserInfo(subject, email, emailVerified, name);
    }

    private static bool IsValidSignedIdTokenWithNonce(JsonWebToken jwt, string expectedNonce)
    {
        if (
            string.IsNullOrWhiteSpace(jwt.Alg)
            || string.Equals(jwt.Alg, "none", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        var tokenNonce = FindClaimValue(jwt, "nonce");
        return !string.IsNullOrWhiteSpace(expectedNonce)
            && !string.IsNullOrWhiteSpace(tokenNonce)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(tokenNonce),
                Encoding.UTF8.GetBytes(expectedNonce)
            );
    }

    private static string? FindClaimValue(JsonWebToken jwt, string claimType)
    {
        return jwt
            .Claims.FirstOrDefault(c => string.Equals(c.Type, claimType, StringComparison.Ordinal))
            ?.Value;
    }

    private sealed record OidcDiscoveryDocument(
        [property: JsonPropertyName("authorization_endpoint")] string? AuthorizationEndpoint,
        [property: JsonPropertyName("token_endpoint")] string? TokenEndpoint
    );
}
