using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Registered OAuth/OIDC client application.</summary>
public sealed class AuthClient : AggregateRoot<AuthClientId>, IAuditableEntity, IHasConcurrencyToken
{
    private AuthClient() { }

    private AuthClient(
        AuthClientId id,
        string name,
        string clientId,
        string? clientSecretHash,
        string redirectUri,
        string postLogoutRedirectUri,
        string allowedScopes,
        string allowedClaims,
        bool allowUserApiAccess,
        int scopeApprovalExpirationDays
    )
        : base(id)
    {
        Name = name;
        ClientId = clientId;
        ClientSecretHash = clientSecretHash;
        RedirectUri = redirectUri;
        PostLogoutRedirectUri = postLogoutRedirectUri;
        AllowedScopes = allowedScopes;
        AllowedClaims = allowedClaims;
        AllowUserApiAccess = allowUserApiAccess;
        ScopeApprovalExpirationDays = scopeApprovalExpirationDays;
        IsActive = true;
    }

    /// <summary>Gets the client display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the unique OAuth/OIDC client identifier.</summary>
    public string ClientId { get; private set; } = string.Empty;

    /// <summary>Gets the hashed client secret, if confidential.</summary>
    public string? ClientSecretHash { get; private set; }

    /// <summary>Gets the registered redirect URI.</summary>
    public string RedirectUri { get; private set; } = string.Empty;

    /// <summary>Gets the registered post-logout redirect URI.</summary>
    public string PostLogoutRedirectUri { get; private set; } = string.Empty;

    /// <summary>Gets the space-delimited allowed scopes.</summary>
    public string AllowedScopes { get; private set; } = string.Empty;

    /// <summary>Gets the comma-delimited allowed claims.</summary>
    public string AllowedClaims { get; private set; } = string.Empty;

    /// <summary>Gets whether the client may access user APIs.</summary>
    public bool AllowUserApiAccess { get; private set; }

    /// <summary>Gets the scope approval expiration period in days.</summary>
    public int ScopeApprovalExpirationDays { get; private set; } = 365;

    /// <summary>Gets whether the client is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates a validated OAuth/OIDC client.</summary>
    public static Result<AuthClient> Create(
        string name,
        string clientId,
        string? clientSecretHash,
        string redirectUri,
        string postLogoutRedirectUri,
        string allowedScopes,
        string allowedClaims,
        bool allowUserApiAccess,
        int scopeApprovalExpirationDays = 365
    )
    {
        return
            string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(clientId)
            || clientId.Trim().Length > 50
            || !SafeRedirectValidator.IsSafeRegisteredRedirectUri(redirectUri)
            || !SafeRedirectValidator.IsSafeRegisteredRedirectUri(postLogoutRedirectUri)
            || scopeApprovalExpirationDays < 0
            ? (Result<AuthClient>)IdentityErrors.AuthClientInvalid
            : (Result<AuthClient>)
                new AuthClient(
                    AuthClientId.Create(),
                    name.Trim(),
                    clientId.Trim(),
                    clientSecretHash,
                    redirectUri.Trim(),
                    postLogoutRedirectUri.Trim(),
                    allowedScopes?.Trim() ?? string.Empty,
                    allowedClaims?.Trim() ?? string.Empty,
                    allowUserApiAccess,
                    scopeApprovalExpirationDays
                );
    }

    /// <summary>Verifies an exact RFC 9700 ordinal match against <see cref="RedirectUri" />.</summary>
    public bool MatchesRedirectUri(string? candidateRedirectUri)
    {
        return IsActive
            && SafeRedirectValidator.MatchesExactRedirectUri(RedirectUri, candidateRedirectUri);
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
