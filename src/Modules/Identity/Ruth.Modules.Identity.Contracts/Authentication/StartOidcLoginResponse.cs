namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Contains the authorization URL and anti-CSRF state for an initiated external OIDC login flow.</summary>
public sealed record StartOidcLoginResponse(Guid SessionId, string AuthorizationUrl, string State);
